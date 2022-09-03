using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Favourite_Photo_Browser
{
    internal static class StaticImages
    {
        public static Bitmap PhotoLoadingImage { get; private set; }
        public static Bitmap IconFavouriteUnknown { get; private set; }
        public static Bitmap IconFavouriteOn { get; private set; }
        public static Bitmap UnknownFormat { get; private set; }

        static StaticImages()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>()!;

            UnknownFormat = new Bitmap(assets.Open(new Uri($"avares://{assemblyName}/Assets/unknown.png")));
            PhotoLoadingImage = new Bitmap(assets.Open(new Uri($"avares://{assemblyName}/Assets/loading.png")));
            IconFavouriteUnknown = new Bitmap(assets.Open(new Uri($"avares://{assemblyName}/Assets/favourite-unknown.png")));
            IconFavouriteOn = new Bitmap(assets.Open(new Uri($"avares://{assemblyName}/Assets/favourite-on.png")));
        }

    }
}
