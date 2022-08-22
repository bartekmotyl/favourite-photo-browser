using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Favourite_Photo_Browser
{
    internal record ImageWithThumbnail 
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int FileSize { get; set; }
        public DateTime FileDate { get; set; }
        public DateTime ExifDate { get; set; }
        public byte[] Data { get; set; }
        public byte[] ThumbnailData { get; set; }
        public int ThumbnailWidth { get; set; }
        public int ThumbnailHeight { get; set; }
        public string HashSha1 { get; set; }

        public ImageWithThumbnail(string path, string fileName, byte[] data, byte[] thumbnailData, string hashSha1)
        {
            Path = path;
            FileName = fileName;
            Data = data;
            ThumbnailData = thumbnailData;
            HashSha1 = hashSha1;
        }
    }

    internal class ImageProcessor
    {
        private SHA1 sha1 = SHA1.Create();

        public async Task<ImageWithThumbnail> GenerateThumbnail(string folder, string fileName, int thumbnailSize)
        {
            try
            {
                var path = Path.Combine(folder, fileName);
                var fileInfo = new FileInfo(path);
                var bytes = await File.ReadAllBytesAsync(path);
                using (Image image = Image.Load(bytes))
                {
                    int width = image.Width;
                    int height = image.Height;

                    /*
                    var exifDateStr = image.Metadata.ExifProfile.GetValue(ExifTag.DateTimeOriginal);
                    DateTime? exifDateTime = null;
                    DateTime.TryParse(exifDateStr.Value ?? "", "yyyy-MM-dd HH-mm-ss", out exifDateTime);
                    */
                    int expectedThumbnailWidth = width > height ? thumbnailSize : 0;
                    int expectedThumbnailHeight = width > height ? 0 : thumbnailSize;

                    image.Mutate(img => img.Resize(expectedThumbnailWidth, expectedThumbnailHeight));
                    byte[] bytesThumbnail;
                    using (var stream = new MemoryStream())
                    {
                        await image.SaveAsJpegAsync(stream);
                        bytesThumbnail = stream.ToArray();
                    }
                    var hashBytes = sha1.ComputeHash(bytes);
                    var hashSha1 = Convert.ToHexString(hashBytes).ToLower();
                    return new ImageWithThumbnail(path, fileName, bytes, bytesThumbnail, hashSha1)
                    {
                        FileSize = bytes.Length,
                        FileDate = fileInfo.LastWriteTime,
                        Width = width,
                        Height = height,
                        ThumbnailWidth = image.Width,
                        ThumbnailHeight = image.Height,
                    };
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
