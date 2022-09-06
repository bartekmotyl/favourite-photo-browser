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
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;

            KeyDown += MainWindow_KeyDown;
            DataContextChanged += MainWindow_DataContextChanged;
        }

        private void MainWindow_DataContextChanged(object? sender, EventArgs e)
        {
            if (ViewModel != null)
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.CurrentFolderPath))
            {
                thumbnailsScrollViewer.Offset = Vector.Zero;
            }
            if (e.PropertyName== nameof(MainWindowViewModel.TargetImage))
            {
                zoomBorderImage.ResetMatrix();
                EnsureItemVisibleInScrollViewer();
            }
        }

        private async void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                await ViewModel.NavigateToImage(1);
            }
            else if (e.Key == Key.Left)
            {
                await ViewModel.NavigateToImage(-1);
            }

            switch (e.Key)
            {
                case Key.R:
                    zoomBorderImage.ResetMatrix();
                    break;
                case Key.F:
                    await ViewModel.ToggleFavourite();
                    break;
            }
        }

        private async void OpenFolderButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await ViewModel.OpenFolder(StorageProvider);
        }

        private void CopyFavouritesPaths_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ViewModel.CopyFavouritePathsToClipboard();
        }

        private async void OnThumbnailImageClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var folderItem = ((e.Source as Button)!.DataContext as FolderItemViewModel)!;

            if (folderItem.Ignored)
                return;

            await ViewModel.ChangeCurrentFolderItem(folderItem);
          
        }
        private void OnThumbnailScrollViewPointerWheelChanged(object sender, PointerWheelEventArgs e) 
        {
            thumbnailsScrollViewer.Offset = new Avalonia.Vector(thumbnailsScrollViewer.Offset.X - 
                e.Delta.Y * thumbnailsScrollViewer.LargeChange.Width/2, 0);
        }
        private async void FavouriteToggle_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await ViewModel.ToggleFavourite();
        }

        // TODO: fix the code below - it doesn't seem to work with Avalonia 11
        private void EnsureItemVisibleInScrollViewer()
        {
            var index = ViewModel.CurrentFolderItemIndex;
            if (index == null)
                return;

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
