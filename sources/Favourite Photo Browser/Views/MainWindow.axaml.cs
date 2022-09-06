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





        private MainWindowViewModel? ViewModel => (MainWindowViewModel?)DataContext;

        public MainWindow()
        {
            InitializeComponent();
  
            this.KeyDown += MainWindow_KeyDown;
            
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
                    ViewModel?.ToggleFavourite();
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

            if (ViewModel!.CurrentFolderItem != null)
                ViewModel!.CurrentFolderItem.IsActive = false;
            
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
            ViewModel?.ToggleFavourite();
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

            ViewModel!.CurrentFolderPath = folderUri.AbsolutePath;

            thumbnailsScrollViewer.Offset = Avalonia.Vector.Zero;
            var viewModel = ViewModel!;

            await Task.Run(async () =>
            {
                await viewModel.LoadFilesInFolder(folderUri.AbsolutePath);
            });
        }


        private void NavigateToImage(int offset)
        {
            var currentFolderItem = ViewModel!.CurrentFolderItem;

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
        private void EnsureItemVisibleInScrollViewer(int index)
        {
            // TODO: fix the code below - it doesn't seem to work with Avalonia 11
            /*
            var control = thumbnailsItemRepeater.TryGetElement(0);
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
            */
        }

        private void SetCurrentImage(int index)
        {

            if (ViewModel!.CurrentFolderItem != null)
                ViewModel!.CurrentFolderItem.IsActive = false;

            ViewModel!.CurrentFolderItem = ViewModel!.FolderItems[index];

            var currentFolderItem = ViewModel!.CurrentFolderItem;
            currentFolderItem.IsActive = true;

            EnsureItemVisibleInScrollViewer(index);
            
            Task.Run(async () =>
            {

                var pathToLoad = currentFolderItem.Path;
                var bitmap = new Bitmap(pathToLoad); // this takes time, especially on network drive 
                // the if is here to ignore loaded image if we already requested another one 
                if (pathToLoad == currentFolderItem.Path)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        zoomBorderImage.ResetMatrix();
                        targetImage.Source = bitmap;
                    });
                }
            });
        }
    }
}
