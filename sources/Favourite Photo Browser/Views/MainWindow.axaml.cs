using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.ComponentModel;
using Avalonia;

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

            this.DataContextChanged += MainWindow_DataContextChanged;

            
        }

        private void MainWindow_DataContextChanged(object? sender, EventArgs e)
        {
            if (ViewModel != null)
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentFolderPath")
            {
                thumbnailsScrollViewer.Offset = Vector.Zero;
            }
            if (e.PropertyName=="TargetImage")
            {
                zoomBorderImage.ResetMatrix();
                EnsureItemVisibleInScrollViewer();
            }
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                ViewModel?.NavigateToImage(1);
            }
            else if (e.Key == Key.Left)
            {
                ViewModel?.NavigateToImage(-1);
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
            ViewModel!.OpenFolder(StorageProvider);
        }

        private void CopyFavouritesPaths_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ViewModel?.CopyFavouritePathsToClipboard();
        }
        

        private void OnThumbnailImageClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var thumbnailData = ((e.Source as Button)!.DataContext as FolderItemViewModel)!;

            if (thumbnailData.Ignored)
                return;

            if (ViewModel!.CurrentFolderItem != null)
                ViewModel!.CurrentFolderItem.IsActive = false;
            
            var index = ViewModel!.FolderItems.IndexOf(thumbnailData);
            ViewModel!.SetCurrentImage(index);
          
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

        public void EnsureItemVisibleInScrollViewer()
        {
            var index = ViewModel!.CurrentFolderItemIndex;
            if (index == null)
                return;

            // TODO: fix the code below - it doesn't seem to work with Avalonia 11
            var control = thumbnailsItemRepeater.TryGetElement(index.Value);
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
        }


    }
}
