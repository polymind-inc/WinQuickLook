using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace WinQuickLook.Converters;

[ValueConversion(typeof(FileInfo), typeof(string))]
public class FileInfoToSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FileInfo fileInfo)
        {
            return GetSizeFormat(fileInfo.Length);
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => DependencyProperty.UnsetValue;

    private const long TeraByte = 1024L * 1024 * 1024 * 1024;
    private const long GigaByte = 1024L * 1024 * 1024;
    private const long MegaByte = 1024L * 1024;
    private const long KiroByte = 1024L;

    private static string GetSizeFormat(long length) => length switch
    {
        >= TeraByte => $"{length / (double)TeraByte:0.##} TB",
        >= GigaByte => $"{length / (double)GigaByte:0.##} GB",
        >= MegaByte => $"{length / (double)MegaByte:0.##} MB",
        >= KiroByte => $"{length / (double)KiroByte:0.##} KB",
        _ => $"{length} B"
    };
}
