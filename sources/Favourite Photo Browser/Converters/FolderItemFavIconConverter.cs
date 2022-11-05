using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Utilities;
using Favourite_Photo_Browser.ViewModels;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Favourite_Photo_Browser.Converters
{
    public class FolderItemFavIconConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Count != 2)
                return AvaloniaProperty.UnsetValue;



            if (!TypeUtilities.CanCast<int?>(values[0]))
                return AvaloniaProperty.UnsetValue;

            if (!TypeUtilities.CanCast<int>(values[1]))
                return AvaloniaProperty.UnsetValue;

            var favourite = (int?)values[0];
            var mask = (int?)values[1] ?? 1;

            bool? matches = FolderItemViewModel.MatchesFavourite(favourite, mask);

            if (matches == null)
                return StaticImages.IconFavouriteUnknown;

            return matches.Value ? StaticImages.IconFavouriteOn : null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
