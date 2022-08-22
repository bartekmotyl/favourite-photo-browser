using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            PhotoLoadingImage = GetBitmapFromResourceBitmap(Resources.loading);
            IconFavouriteUnknown = GetBitmapFromResourceBitmap(Resources.favourite_unknown);
            IconFavouriteOn = GetBitmapFromResourceBitmap(Resources.favourite_on);
            UnknownFormat = GetBitmapFromResourceBitmap(Resources.unknown_format);
        }

        private static Bitmap GetBitmapFromResourceBitmap(System.Drawing.Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);
                return new Bitmap(stream);
            }
        }
    }
}
