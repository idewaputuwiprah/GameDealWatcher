using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace GameDealWatcher.App.Converters;

public class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null) return string.Empty;
        if (parameter is string format && !string.IsNullOrEmpty(format))
            return string.Format(format, value);
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
