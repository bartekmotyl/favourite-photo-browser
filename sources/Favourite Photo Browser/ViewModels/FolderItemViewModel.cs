using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Linq;

namespace Favourite_Photo_Browser.ViewModels
{
    public class FolderItemViewModel : ViewModelBase
    {
        private readonly string[] acceptedFileExtensions = new string[] { "jpg", "png", "tiff", "jpeg" /*, "arw"*/ };

        public FolderItemViewModel(string path, string fileName, DateTime fileDate)
        {
            this.path = path;
            this.fileName = fileName;
            this.fileDate = fileDate;
            title = fileName;
            ignored = acceptedFileExtensions.All(ext => !path.ToLower().EndsWith(ext));
            if (ignored)
            {
                thumbnailImage = StaticImages.UnknownFormat;
                favourite = 0;
                favouriteIcon = null;
            }
        }

        private string path;
        private readonly string fileName;
        private readonly DateTime fileDate;
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
        public DateTime FileDate => fileDate;

        public bool IsActive
        {
            get => isActive;
            set
            {
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
}
