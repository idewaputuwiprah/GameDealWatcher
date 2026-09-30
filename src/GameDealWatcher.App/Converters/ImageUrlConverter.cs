using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameDealWatcher.App.Converters;

public class ImageUrlConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string url && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return new BitmapImage(uri);
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
