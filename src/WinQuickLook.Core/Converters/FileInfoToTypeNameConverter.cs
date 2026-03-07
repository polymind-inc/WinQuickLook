using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace WinQuickLook.Converters;

[ValueConversion(typeof(FileSystemInfo), typeof(string))]
public class FileInfoToTypeNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not FileSystemInfo fileSystemInfo)
        {
            return DependencyProperty.UnsetValue;
        }

        var sfi = new SHFILEINFOW();

        PInvoke.SHGetFileInfo(fileSystemInfo.FullName, 0, ref sfi, SHGFI_FLAGS.SHGFI_TYPENAME | SHGFI_FLAGS.SHGFI_USEFILEATTRIBUTES);

        return sfi.szTypeName;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
}
