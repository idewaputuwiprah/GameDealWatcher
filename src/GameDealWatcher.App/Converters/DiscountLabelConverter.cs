using System;
using Microsoft.UI.Xaml.Data;

namespace GameDealWatcher.App.Converters;

public class DiscountLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int discount ? $"-{discount}%" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
