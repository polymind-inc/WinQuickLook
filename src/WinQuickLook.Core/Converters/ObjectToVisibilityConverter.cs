using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WinQuickLook.Converters;

[ValueConversion(typeof(object), typeof(Visibility))]
public class ObjectToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => DependencyProperty.UnsetValue;

    private static bool ToBoolean(object? value)
    {
        if (value is null)
        {
            return false;
        }

        return value switch
        {
            string stringValue => !string.IsNullOrEmpty(stringValue),
            ICollection collection => collection.Count != 0,
            _ => true
        };
    }
}
