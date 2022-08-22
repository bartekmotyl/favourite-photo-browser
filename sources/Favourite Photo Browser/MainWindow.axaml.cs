using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MessageBox.Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TextCopy;
using Avalonia;

namespace Favourite_Photo_Browser
{
    internal class FolderItemInfo : ReactiveObject
    {
        private readonly string[] acceptedFileExtensions = new string[] { "jpg", "png", "tiff", "jpeg" /*, "arw"*/ };

        public FolderItemInfo(string path, string fileName)
        {
            this.path = path;
            this.fileName = fileName;
            this.title = fileName;
            this.ignored = acceptedFileExtensions.All(ext => !path.ToLower().EndsWith(ext));
            if (ignored)
            {
                this.thumbnailImage = StaticImages.UnknownFormat;
                this.favourite = 0;
                this.favouriteIcon = null;
            }
        }

        private string path;
        private readonly string fileName;
        private string title;
        private bool isLoaded;
        private Bitmap thumbnailImage = StaticImages.PhotoLoadingImage;
        private Bitmap? favouriteIcon = StaticImages.IconFavouriteUnknown;
        private bool isActive;
        public IBrush borderBrush = Brushes.Transparent;
        private int? imageId;
        private int? favourite;
        private readonly bool ignored = false;


        public string Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }
        public string Title { get => title; set => this.RaiseAndSetIfChanged(ref title, value); }
        public bool IsLoaded { get => isLoaded; set => this.RaiseAndSetIfChanged(ref isLoaded, value); }
        public Bitmap ThumbnailImage { get => thumbnailImage; set => this.RaiseAndSetIfChanged(ref thumbnailImage, value); }
        public Bitmap? FavouriteIcon { get => favouriteIcon; set => this.RaiseAndSetIfChanged(ref favouriteIcon, value); }
        public IBrush BorderBrush { get => borderBrush; set => this.RaiseAndSetIfChanged(ref borderBrush, value); }
        public int? ImageId { get => imageId; set => this.RaiseAndSetIfChanged(ref imageId, value); }
        public bool Ignored => ignored;
        public string FileName => fileName;

        public bool IsActive {
            get => isActive;
            set {
                this.RaiseAndSetIfChanged(ref isActive, value);
                BorderBrush = value ? Brushes.Tomato : Brushes.Transparent;
            }
        }

        

        public int? Favourite
        {
            get => favourite;
            set
            {
                this.RaiseAndSetIfChanged(ref favourite, value);
                if (value == null)
                {
                    FavouriteIcon = StaticImages.IconFavouriteUnknown;
                }
                else
                {
                    FavouriteIcon = value == 0 ? null : StaticImages.IconFavouriteOn;
                }
            }
        }
    }

    public partial class MainWindow : Window
    {
        private string? currentFolder = null;
        private readonly DBConnector dbConnector;
        private ThumnailsLoadingJob? thumbnailsLoadingJob = null;
        private FolderItemInfo? currentFolderItem = null;
        private readonly AvaloniaList<FolderItemInfo>  folderItems = new();

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif
            this.dbConnector = new DBConnector(@"photos.db");
            
            this.KeyDown += MainWindow_KeyDown;
            this.openFolderButton.Click += OpenFolderButton_Click;
            
            thumbnailsItemRepeater.Items = folderItems;
            zoomBorderImage.KeyDown += ZoomBorderImage_KeyDown;
            WindowState = WindowState.Maximized;
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
            var paths = folderItems?.Where(fi => (fi.Favourite ?? 0) > 0).Select(fi => fi.Path).ToArray();
            if (paths == null)
                return;
            var text = string.Join(Environment.NewLine, paths);
            ClipboardService.SetText(text);
        }
        

        private void OnThumbnailImageClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var thumbnailData = ((e.Source as Button)!.DataContext as FolderItemInfo)!;

            if (thumbnailData.Ignored)
                return;

            if (currentFolderItem != null)
                currentFolderItem.IsActive = false;

            var index = folderItems.IndexOf(thumbnailData);
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
   
            var openFolderDialog = new OpenFolderDialog()
            {
                Title = "Open Folder Dialog",
            };
            var path = await openFolderDialog.ShowAsync(this);
            this.currentFolder = path;
            thumbnailsScrollViewer.Offset = Avalonia.Vector.Zero;

            await Task.Run(async () =>
            {
                await LoadFilesInFolder();
            });
        }

        private async Task LoadFilesInFolder()
        {
            
            var directory = new DirectoryInfo(this.currentFolder!);
            var files = directory.GetFiles().OrderBy(f => f.Name.ToUpperInvariant()).ToList();
            

            
            var allFolderItems = files.Select(fileInfo => new FolderItemInfo(fileInfo.FullName, fileInfo.Name)).ToList();


            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                folderItems.Clear();
                folderItems.AddRange(allFolderItems);
                this.textCurrentFolder.Text = this.currentFolder;
            });
            this.thumbnailsLoadingJob?.CancellationTokenSource.Cancel();



            var fileNamesToProcess = allFolderItems.Where(fi => !fi.Ignored).Select(fi => fi.FileName).ToArray();
            this.thumbnailsLoadingJob = new ThumnailsLoadingJob(this.currentFolder!, fileNamesToProcess);


            var readThumnailsTask = dbConnector.ReadThumbnails(thumbnailsLoadingJob);

            var processLoadedImagesAsync = () =>
            {
                while (!thumbnailsLoadingJob.ProcessingProgress.IsEmpty)
                {
                    if (thumbnailsLoadingJob.ProcessingProgress.TryDequeue(out var thumbnailLoadingStatus))
                    {
                        var thumbnail = folderItems.FirstOrDefault(t => t.Title == thumbnailLoadingStatus.FileName);
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
            var index = folderItems.IndexOf(currentFolderItem);
            var newIndex = index + offset;
            int direction = offset < 0 ? -1 : 1;

            while (true)
            {
                if (newIndex < 0 || newIndex >= folderItems.Count)
                {
                    newIndex = index;
                    break;
                }
                if (!folderItems[newIndex].Ignored)
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

            currentFolderItem = folderItems[index];
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
