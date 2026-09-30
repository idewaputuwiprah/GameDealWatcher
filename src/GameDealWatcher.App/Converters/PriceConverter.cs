using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace GameDealWatcher.App.Converters;

public class PriceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal price)
            return price <= 0 ? "Free" : price.ToString("$0.00", CultureInfo.InvariantCulture);
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
