using System;
using System.Windows;

namespace WinQuickLook.Extensions;

internal static class SizeExtensions
{
    public static Size FitTo(this Size size, int maxWidthOrHeight)
    {
        if (size.Width <= maxWidthOrHeight && size.Height <= maxWidthOrHeight)
        {
            return size;
        }

        var scaleFactor = maxWidthOrHeight / Math.Max(size.Width, size.Height);

        return new Size(size.Width * scaleFactor, size.Height * scaleFactor);
    }
}
