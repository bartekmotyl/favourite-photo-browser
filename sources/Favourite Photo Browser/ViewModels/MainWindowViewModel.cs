using Avalonia.Collections;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Favourite_Photo_Browser.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly DBConnector dbConnector;
        private ThumnailsLoadingJob? thumbnailsLoadingJob = null;

        private readonly AvaloniaList<FolderItemViewModel> folderItems = new();
        private string currentFolderPath = "(not slected)";
        private FolderItemViewModel? currentFolderItem = null;

        public AvaloniaList<FolderItemViewModel> FolderItems => folderItems;
        public string CurrentFolderPath { get => currentFolderPath; set => this.RaiseAndSetIfChanged(ref currentFolderPath, value); }
        public FolderItemViewModel? CurrentFolderItem { get => currentFolderItem; set => this.RaiseAndSetIfChanged(ref currentFolderItem, value); }


        public MainWindowViewModel()
        {
            this.dbConnector = new DBConnector(@"photos.db");
        }

        public void ToggleFavourite()
        {
            if (currentFolderItem == null)
                return;

            Task.Run(async () =>
            {
                var updated = await dbConnector.ToggleFavourite(currentFolderItem!.ImageId!.Value);
                currentFolderItem.Favourite = updated;
            });
        }


        public async Task LoadFilesInFolder(string folderPath)
        {

            var directory = new DirectoryInfo(folderPath);
            var files = directory.GetFiles().OrderBy(f => f.CreationTimeUtc).ToList();



            var allFolderItems = files.Select(fileInfo => new FolderItemViewModel(fileInfo.FullName, fileInfo.Name)).ToList();


            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                FolderItems.Clear();
                FolderItems.AddRange(allFolderItems);
            });
            this.thumbnailsLoadingJob?.CancellationTokenSource.Cancel();



            var fileNamesToProcess = allFolderItems.Where(fi => !fi.Ignored).Select(fi => fi.FileName).ToArray();
            this.thumbnailsLoadingJob = new ThumnailsLoadingJob(folderPath, fileNamesToProcess);

            var readThumnailsTask = dbConnector.ReadThumbnails(thumbnailsLoadingJob);

            var processLoadedImagesAsync = () =>
            {
                while (!thumbnailsLoadingJob.ProcessingProgress.IsEmpty)
                {
                    if (thumbnailsLoadingJob.ProcessingProgress.TryDequeue(out var thumbnailLoadingStatus))
                    {

                        var thumbnail = folderItems!.FirstOrDefault(t => t.Title == thumbnailLoadingStatus.FileName);
                        if (thumbnail != null)
                        {
                            var data = thumbnailLoadingStatus.LoadedThumbnail!.Data;
                            var bitmap = new Bitmap(new MemoryStream(data));
                            thumbnail.ThumbnailImage = bitmap;
                            thumbnail.IsLoaded = true;
                            thumbnail.ImageId = thumbnailLoadingStatus.LoadedThumbnail!.ImageId;
                            thumbnail.Favourite = thumbnailLoadingStatus.LoadedThumbnail!.Favourite;
                        }

                    };
                }
            };


            while (!readThumnailsTask.IsCompleted)
            {
                if (thumbnailsLoadingJob.CancellationTokenSource.IsCancellationRequested)
                    break;

                if (!thumbnailsLoadingJob.ProcessingProgress.IsEmpty)
                    processLoadedImagesAsync();
                else
                    await Task.Delay(100);
            }

            if (!thumbnailsLoadingJob.ProcessingProgress.IsEmpty)
                processLoadedImagesAsync();
        }

    }
}
