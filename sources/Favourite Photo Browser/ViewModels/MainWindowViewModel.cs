using Avalonia.Collections;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextCopy;

namespace Favourite_Photo_Browser.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly DBConnector dbConnector;
        private ThumnailsLoadingJob? thumbnailsLoadingJob = null;

        private readonly AvaloniaList<FolderItemViewModel> folderItems = new();
        private string currentFolderPath = "(not selected)";
        private FolderItemViewModel? currentFolderItem = null;
        private Bitmap? targetImage = null;
        private int selectedSortOrderIndex = 0;

        public AvaloniaList<FolderItemViewModel> FolderItems => folderItems;
        public string CurrentFolderPath { get => currentFolderPath; set => this.RaiseAndSetIfChanged(ref currentFolderPath, value); }
        public FolderItemViewModel? CurrentFolderItem { get => currentFolderItem; set => this.RaiseAndSetIfChanged(ref currentFolderItem, value); }
        public Bitmap? TargetImage { get => targetImage; set => this.RaiseAndSetIfChanged(ref targetImage, value); }
        public int? CurrentFolderItemIndex => CurrentFolderItem == null ? null : folderItems.IndexOf(CurrentFolderItem);

        public int SelectedSortOrderIndex
        {
            get => selectedSortOrderIndex; 
            set
            {
                this.RaiseAndSetIfChanged(ref selectedSortOrderIndex, value);
                UpdateThumbnailsSorting();
            }
        }


        public MainWindowViewModel()
        {
            this.dbConnector = new DBConnector(@"photos.db");
        }

        
        public async Task ToggleFavourite()
        {
            if (currentFolderItem == null)
                return;

            var updated = await dbConnector.ToggleFavourite(currentFolderItem!.ImageId!.Value);
            currentFolderItem.Favourite = updated;
        }

        private void UpdateThumbnailsSorting()
        {
            IEnumerable<FolderItemViewModel> sorted;
            if (SelectedSortOrderIndex == 0)
                sorted = FolderItems.OrderBy(f => f.FileDate);
            else if (SelectedSortOrderIndex == 1)
                sorted = FolderItems.OrderByDescending(f => f.FileDate);
            else if (SelectedSortOrderIndex == 2)
                sorted = FolderItems.OrderBy(f => f.FileName);
            else 
                sorted = FolderItems.OrderByDescending(f => f.FileName);

            var newItems = sorted.ToList();

            FolderItems.Clear();
            FolderItems.AddRange(newItems);
        }

        private async Task LoadFilesInFolder(string folderPath)
        {

            var directory = new DirectoryInfo(folderPath);
            var files = directory.GetFiles().ToList();
            var allFolderItems = files.Select(fileInfo => new FolderItemViewModel(
                fileInfo.FullName, fileInfo.Name, fileInfo.CreationTimeUtc)).ToList();
            
            CurrentFolderItem = null;
            TargetImage = null;
            FolderItems.Clear();
            FolderItems.AddRange(allFolderItems);
            UpdateThumbnailsSorting();

            thumbnailsLoadingJob?.CancellationTokenSource.Cancel();

            var fileNamesToProcess = allFolderItems.Where(fi => !fi.Ignored).Select(fi => fi.FileName).ToArray();
            thumbnailsLoadingJob = new ThumnailsLoadingJob(folderPath, fileNamesToProcess);

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


        public async Task OpenFolder(IStorageProvider storageProvider)
        {
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Select folder",
                AllowMultiple = false,
            });

            if (folders.Count == 0)
                return;

            var folder = folders[0] as IStorageFolder;
            if (!folder.TryGetUri(out Uri? folderUri))
                return;

            CurrentFolderPath = folderUri.AbsolutePath;

            await LoadFilesInFolder(folderUri.AbsolutePath);
        }

        public void CopyFavouritePathsToClipboard()
        {
            var paths = FolderItems?.Where(fi => (fi.Favourite ?? 0) > 0).Select(fi => fi.Path).ToArray();
            if (paths == null)
                return;
            var text = string.Join(Environment.NewLine, paths);
            ClipboardService.SetText(text);
        }


        public async Task ChangeCurrentFolderItem(FolderItemViewModel newCurrentItem)
        {
            if (CurrentFolderItem != null)
                CurrentFolderItem.IsActive = false;

            CurrentFolderItem = newCurrentItem;
            CurrentFolderItem.IsActive = true;

            var pathToLoad = CurrentFolderItem.Path;
            var bitmap = new Bitmap(pathToLoad); // this takes time, especially on network drive 
            // the if is here to ignore loaded image if we already requested another one 
            if (pathToLoad == CurrentFolderItem.Path)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TargetImage = bitmap;
                });
            }
        }

        public async Task NavigateToImage(int offset)
        {
            if (CurrentFolderItem == null)
                return;
            var index = FolderItems.IndexOf(CurrentFolderItem);
            var newIndex = index + offset;
            int direction = offset < 0 ? -1 : 1;

            while (true)
            {
                if (newIndex < 0 || newIndex >= FolderItems.Count)
                {
                    newIndex = index;
                    break;
                }
                if (!FolderItems[newIndex].Ignored)
                    break;
                newIndex += direction;
            }
            await ChangeCurrentFolderItem(FolderItems[newIndex]);
        }
    }
}
