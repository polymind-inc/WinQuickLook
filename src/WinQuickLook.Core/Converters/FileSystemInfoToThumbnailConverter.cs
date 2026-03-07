using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

using WinQuickLook.Providers;

namespace WinQuickLook.Converters;

[ValueConversion(typeof(FileSystemInfo), typeof(BitmapSource))]
public class FileSystemInfoToThumbnailConverter : IValueConverter
{
    private static readonly ShellThumbnailProvider s_shellThumbnailProvider = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FileSystemInfo fileSystemInfo)
        {
            return s_shellThumbnailProvider.GetImage(fileSystemInfo)!;
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
}
