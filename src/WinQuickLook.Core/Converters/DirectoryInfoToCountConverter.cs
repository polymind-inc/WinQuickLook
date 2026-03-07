using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace WinQuickLook.Converters;

[ValueConversion(typeof(DirectoryInfo), typeof(int))]
public class DirectoryInfoToCountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DirectoryInfo directoryInfo)
        {
            return directoryInfo.EnumerateFileSystemInfos().Count();
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
}
