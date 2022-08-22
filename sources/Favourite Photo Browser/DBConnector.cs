using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib;
using System.Collections.Concurrent;
using Dapper.Contrib.Extensions;
using Avalonia.Media;
using System.Security.Cryptography;

namespace Favourite_Photo_Browser
{
    [Table("Folder")]
    internal record FolderEntity
    {
        [ExplicitKey]
        public int? FolderId { get; set; }
        public string? FolderPath { get; set; }
    }

    [Table("Image")]
    internal record ImageEntity
    {
        [ExplicitKey]
        public int? ImageId { get; set; }
        public int? FolderId { get; set; }
        public string? FileName { get; set; }
        public int FileTime { get; set; }
        public int ExifTime { get; set; }
        public int FileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ThumbnailSize { get; set; }
        public byte[]? Thumbnail { get; set; }
        public int Favourite { get; set; }
        public string? HashSha1 { get; set; }

        public ImageEntity()
        {
        }
        public ImageEntity(ImageWithThumbnail imageWithThumbnail, int folderId, string fileName, string hashSha1)
        {
            FolderId = folderId;
            FileName = fileName;
            HashSha1 = hashSha1;
            FileTime = (int)imageWithThumbnail.FileDate.Ticks / 1000;
            ExifTime = 0; /* @todo */
            FileSize = imageWithThumbnail.FileSize;
            Width = imageWithThumbnail.Width;
            Height = imageWithThumbnail.Height;
            ThumbnailSize = Math.Max(imageWithThumbnail.ThumbnailWidth, imageWithThumbnail.ThumbnailHeight);
            Thumbnail = imageWithThumbnail.ThumbnailData;
        }
    }

    internal record LoadedThumbnail
    {
        public int ImageId { get; set; }
        public int Favourite { get; set; }
        public byte[] Data { get; set; }

        public LoadedThumbnail(int imageId, int favourite, byte[] data)
        {
            ImageId = imageId;
            Favourite = favourite;
            Data = data;
        }
    }

    internal record ThumbnailLoadingStatus
    {


        public ThumbnailLoadingStatus(string fileName)
        {
            FileName = fileName;
        }
        public string FileName { get; set; }
        public LoadedThumbnail? LoadedThumbnail { get; set; } = null;

        public bool RequiresProcessing => LoadedThumbnail == null;
    }

    internal record ThumnailsLoadingJob
    {
        public ThumnailsLoadingJob(string folderPath, string[] filenames)
        {
            FolderPath = folderPath;
            Thumbnails = filenames.Select(filename => new ThumbnailLoadingStatus(filename)).ToArray();
        }
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        public string FolderPath { get; set; }
        public ThumbnailLoadingStatus[] Thumbnails { get; set; }
        public int? FirstVisibleIndex { get; set; }
        public ConcurrentQueue<ThumbnailLoadingStatus> ProcessingProgress { get; set; } = new ConcurrentQueue<ThumbnailLoadingStatus>();

        public void DBThumbnailsLoaded(List<ImageEntity> entries)
        {
            var entriesDictByFileName = entries.ToDictionary(e => e.FileName!, e => e);
            for (var index = 0; index < Thumbnails.Length; index++)
            {
                var thumbnail = Thumbnails[index];
                if (entriesDictByFileName.ContainsKey(thumbnail.FileName))
                {
                    var entry = entriesDictByFileName[thumbnail.FileName];
                    thumbnail.LoadedThumbnail = new LoadedThumbnail(entry.ImageId!.Value, entry.Favourite, entry.Thumbnail!);
                    ProcessingProgress.Enqueue(thumbnail);
                }
            }
        }

        public void ThumbnailCreated(int imageId, ImageWithThumbnail imageWithThumbnail)
        {
            for (var index = 0; index < Thumbnails.Length; index++)
            {
                var thumbnail = Thumbnails[index];
                if (thumbnail.FileName == imageWithThumbnail.FileName)
                {
                    thumbnail.LoadedThumbnail = new LoadedThumbnail(imageId, 0, imageWithThumbnail.ThumbnailData);
                    ProcessingProgress.Enqueue(thumbnail);
                    return;
                }
            }
        }
    }

    internal class DBConnector
    {
        private readonly string databasePath;
        private readonly ImageProcessor imageProcessor = new();


        public DBConnector(string databasePath)
        {
            this.databasePath = databasePath;

            if (!File.Exists(databasePath))
            {
                File.WriteAllBytes(databasePath, Array.Empty<byte>());
                using var connection = new SqliteConnection($"Data Source={databasePath}");
                var sqlCreateTableImage = "CREATE TABLE Image (imageId INTEGER PRIMARY KEY AUTOINCREMENT, folderId INTEGER, fileName TEXT, fileTime INTEGER, exifTime INTEGER, fileSize INTEGER, width INTEGER, height INTEGER, thumbnailSize Integer, thumbnail BLOB, favourite INTEGER, hashSha1 TEXT)";
                var sqlCreateTableFolder = "CREATE TABLE Folder (folderId INTEGER PRIMARY KEY AUTOINCREMENT, folderPath TEXT)";
                connection.Execute(sqlCreateTableImage);
                connection.Execute(sqlCreateTableFolder);
            }
        }

        public async Task ReadThumbnails(ThumnailsLoadingJob job)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={databasePath}");
                await connection.OpenAsync();

                var parametersLoadFolder = new { folderPath = job.FolderPath };
                var folder = await connection.QueryFirstOrDefaultAsync<FolderEntity>(
                        @"select * FROM Folder WHERE folderPath = @folderPath", parametersLoadFolder);

                if (folder != null)
                {
                    var parameters = new { folderId = folder.FolderId };
                    var fileListEntries = await connection.QueryAsync<ImageEntity>(
                        @"SELECT * FROM Image WHERE folderId = @folderId", parameters);
                    job.DBThumbnailsLoaded(fileListEntries.ToList());
                }

                var remaining = job.Thumbnails.Where(t => t.RequiresProcessing).ToList();
                if (remaining.Count == 0)
                    return;

                if (folder == null)
                {
                    folder = new FolderEntity() { FolderPath = job.FolderPath };
                    folder.FolderId = (int)connection.Insert(folder);
                }
                var thumbnailSize = 160;

                while (!job.CancellationTokenSource.IsCancellationRequested)
                {
                    var toProcess = job.Thumbnails.FirstOrDefault(t => t.RequiresProcessing);
                    if (toProcess == null)
                        break;
                    var thumbnailData = await imageProcessor.GenerateThumbnail(job.FolderPath, toProcess.FileName, thumbnailSize);
                    var newEntry = new ImageEntity(thumbnailData, folder.FolderId!.Value, toProcess.FileName, thumbnailData.HashSha1);
                    var imageId = await connection.InsertAsync(newEntry);
                    job.ThumbnailCreated(imageId, thumbnailData);
                    await Task.Delay(10);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<int?> ToggleFavourite(int imageId)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={databasePath}");
                await connection.OpenAsync();
                var image = await connection.QueryFirstOrDefaultAsync<ImageEntity>(
                            @"select * FROM Image WHERE imageId = @imageId", new { imageId });
                if (image == null)
                    return null;

                image.Favourite = image.Favourite == 0 ? 1 : 0;
                await connection.UpdateAsync(image);
                return image.Favourite;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
