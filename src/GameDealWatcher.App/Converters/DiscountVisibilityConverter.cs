using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace GameDealWatcher.App.Converters;

public class DiscountVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int discount && discount > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
