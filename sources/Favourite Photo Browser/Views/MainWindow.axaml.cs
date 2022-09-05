using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MessageBox.Avalonia;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TextCopy;
using Avalonia;
using System.Reactive.Subjects;
using Avalonia.Platform.Storage;
using Favourite_Photo_Browser.ViewModels;

namespace Favourite_Photo_Browser
{

    public partial class MainWindow : Window
    {
        private string? currentFolder = null;
        private readonly DBConnector dbConnector;
        private ThumnailsLoadingJob? thumbnailsLoadingJob = null;
        private FolderItemViewModel? currentFolderItem = null;

        private readonly Subject<string> currentFolderPath = new();
        private MainWindowViewModel? ViewModel => (MainWindowViewModel?)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            this.dbConnector = new DBConnector(@"photos.db");
            
            this.KeyDown += MainWindow_KeyDown;
            
            zoomBorderImage.KeyDown += ZoomBorderImage_KeyDown;
            WindowState = WindowState.Maximized;
            textCurrentFolderPath.Bind(TextBlock.TextProperty, currentFolderPath);
            currentFolderPath.OnNext("(not selected)");
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                NavigateToImage(1);
            }
            else if (e.Key == Key.Left)
            {
                NavigateToImage(-1);
            }

            switch (e.Key)
            {
                case Key.R:
                    zoomBorderImage.ResetMatrix();
                    break;
                case Key.F:
                    ToggleFavourite();
                    break;
            }
        }

        private void ZoomBorderImage_KeyDown(object? sender, KeyEventArgs e)
        {

        }

        private void OpenFolderButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            OpenFolder();
        }

        private void CopyFavouritesPaths_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var paths = ViewModel!.FolderItems?.Where(fi => (fi.Favourite ?? 0) > 0).Select(fi => fi.Path).ToArray();
            if (paths == null)
                return;
            var text = string.Join(Environment.NewLine, paths);
            ClipboardService.SetText(text);
        }
        

        private void OnThumbnailImageClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var thumbnailData = ((e.Source as Button)!.DataContext as FolderItemViewModel)!;

            if (thumbnailData.Ignored)
                return;

            if (currentFolderItem != null)
                currentFolderItem.IsActive = false;
            
            var index = ViewModel!.FolderItems.IndexOf(thumbnailData);
            SetCurrentImage(index);
          
        }
        private void OnThumbnailScrollViewPointerWheelChanged(object sender, PointerWheelEventArgs e) 
        {
            thumbnailsScrollViewer.Offset = new Avalonia.Vector(thumbnailsScrollViewer.Offset.X - 
                e.Delta.Y * thumbnailsScrollViewer.LargeChange.Width/2, 0);
        }
        private void FavouriteToggle_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ToggleFavourite();
        }

        private void ToggleFavourite()
        {
            Task.Run(async () =>
            {
                var updated = await dbConnector.ToggleFavourite(currentFolderItem!.ImageId!.Value);
                currentFolderItem.Favourite = updated;
            });
        }
        private async void OpenFolder()
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Select folder",
                AllowMultiple = false,
            });

            if (folders.Count == 0)
                return;

            var folder = folders.First() as IStorageFolder;
            Uri? folderUri;
            if (!folder.TryGetUri(out folderUri))
                return;

            this.currentFolder = folderUri.AbsolutePath;

            thumbnailsScrollViewer.Offset = Avalonia.Vector.Zero;

            await Task.Run(async () =>
            {
                await LoadFilesInFolder();
            });
        }

        private async Task LoadFilesInFolder()
        {
            
            var directory = new DirectoryInfo(this.currentFolder!);
            var files = directory.GetFiles().OrderBy(f => f.CreationTimeUtc).ToList();
            

            
            var allFolderItems = files.Select(fileInfo => new FolderItemViewModel(fileInfo.FullName, fileInfo.Name)).ToList();


            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ViewModel!.FolderItems.Clear();
                ViewModel!.FolderItems.AddRange(allFolderItems);
                currentFolderPath.OnNext(this.currentFolder ?? "");
                //this.textCurrentFolder.Text = this.currentFolder;
            });
            this.thumbnailsLoadingJob?.CancellationTokenSource.Cancel();



            var fileNamesToProcess = allFolderItems.Where(fi => !fi.Ignored).Select(fi => fi.FileName).ToArray();
            this.thumbnailsLoadingJob = new ThumnailsLoadingJob(this.currentFolder!, fileNamesToProcess);

            AvaloniaList<FolderItemViewModel>? folderItems = null;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                folderItems = ViewModel!.FolderItems;
            });


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

        private void NavigateToImage(int offset)
        {
            if (currentFolderItem == null)
                return;
            var index = ViewModel!.FolderItems.IndexOf(currentFolderItem);
            var newIndex = index + offset;
            int direction = offset < 0 ? -1 : 1;

            while (true)
            {
                if (newIndex < 0 || newIndex >= ViewModel!.FolderItems.Count)
                {
                    newIndex = index;
                    break;
                }
                if (!ViewModel!.FolderItems[newIndex].Ignored)
                    break;
                newIndex += direction;
            }
            SetCurrentImage(newIndex);
        }
        private async Task EnsureItemVisibleInScrollViewer(int index)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var control = thumbnailsItemRepeater.TryGetElement(index);
                if (control != null)
                {
                    var scrollWindowWidth = thumbnailsScrollViewer.Bounds.Width;
                    if (control.Bounds.Left > scrollWindowWidth + thumbnailsScrollViewer.Offset.X)
                    {
                        thumbnailsScrollViewer.Offset = new Vector(control.Bounds.Left - scrollWindowWidth / 2, 0);
                    }
                    if (control.Bounds.Right < thumbnailsScrollViewer.Offset.X)
                    {
                        thumbnailsScrollViewer.Offset = new Vector(control.Bounds.Left - scrollWindowWidth / 2, 0);
                    }
                }
            });
        }

        private void SetCurrentImage(int index)
        {
            if (currentFolderItem != null)
                currentFolderItem.IsActive = false;

            currentFolderItem = ViewModel!.FolderItems[index];
            currentFolderItem.IsActive = true;

            Task.Run(async () =>
            {
                await EnsureItemVisibleInScrollViewer(index);

                var pathToLoad = currentFolderItem.Path;
                var bitmap = new Bitmap(pathToLoad); // this takes time, especially on network drive 
                // the if is here to ignore loaded image if we already requested another one 
                if (pathToLoad == currentFolderItem.Path)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        zoomBorderImage.ResetMatrix();
                        targetImage.Source = bitmap;
                        iconCurrentImageFavourite.DataContext = currentFolderItem;
                    });
                }
            });
        }
    }
}
