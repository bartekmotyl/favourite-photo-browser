using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.ComponentModel;
using Avalonia;
using Favourite_Photo_Browser.ViewModels;
using Avalonia.Media.Transformation;

namespace Favourite_Photo_Browser
{

    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
        private Point? targetImagePressedPoint = null;
        private bool shiftPressed = false;
        private bool controlPressed = false;

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;

            KeyDown += MainWindow_KeyDown;
            KeyUp += MainWindow_KeyUp;
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
                case Key.F:
                    await ViewModel.ToggleFavourite();
                    break;
            }

            shiftPressed = ((e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift);
            controlPressed = ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control);

            UpdateTargetImageTransform();
        }
        private void MainWindow_KeyUp(object? sender, KeyEventArgs e)
        {
            shiftPressed = ((e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift);
            controlPressed = ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control);
            UpdateTargetImageTransform();
        }

        private async void OpenFolderButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await ViewModel.OpenFolder(StorageProvider);
        }

        private void CopyFavouritesPaths_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ViewModel.CopyFavouritePathsToClipboard();
        }

        private async void ThumbnailImage_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var folderItem = ((e.Source as Button)!.DataContext as FolderItemViewModel)!;

            if (folderItem.Ignored)
                return;

            await ViewModel.ChangeCurrentFolderItem(folderItem);
          
        }
        private void ThumbnailScrollViewer_PointerWheelChanged(object sender, PointerWheelEventArgs e) 
        {
            thumbnailsScrollViewer.Offset = new Avalonia.Vector(thumbnailsScrollViewer.Offset.X - 
                e.Delta.Y * thumbnailsScrollViewer.LargeChange.Width/2, 0);
        }
        private async void FavouriteToggle_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await ViewModel.ToggleFavourite();
        }

        private void TargetImage_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            targetImagePressedPoint = e.GetCurrentPoint(targetImage).Position;
            UpdateTargetImageTransform();
        }
        private void TargetImage_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            targetImagePressedPoint = null;
            UpdateTargetImageTransform();
        }
        private void TargetImage_PointerMoved(object sender, PointerEventArgs e)
        {
            if (targetImagePressedPoint != null)
                targetImagePressedPoint = e.GetCurrentPoint(targetImage).Position;
            UpdateTargetImageTransform();
        }
        private void UpdateTargetImageTransform()
        {
            var desiredScale = 2.0;
            desiredScale *= shiftPressed ? 2.0 : 1.0;
            desiredScale *= controlPressed ? 3.0 : 1.0;

            var scale = targetImagePressedPoint.HasValue ? desiredScale : 1.0;
            var matrix = new Matrix(scale, 0.0, 0.0, scale, 0, 0);
            var point = targetImagePressedPoint ?? new Point(0, 0);
            targetImage.RenderTransformOrigin = new RelativePoint(point, RelativeUnit.Absolute);
            var transformBuilder = new TransformOperations.Builder(1);
            transformBuilder.AppendMatrix(matrix);
            targetImage.RenderTransform = transformBuilder.Build();
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
