using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

using WinQuickLook.Extensions;

namespace WinQuickLook.Providers;

public class ShellThumbnailProvider
{
    public BitmapSource? GetImage(FileSystemInfo fileSystemInfo)
    {
        if (PInvoke.SHCreateItemFromParsingName(fileSystemInfo.FullName, null, out IShellItem? shellItem).Failed)
        {
            return null;
        }

        if (shellItem is null)
        {
            return null;
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (shellItem is not IShellItemImageFactory imageFactory)
        {
            Marshal.ReleaseComObject(shellItem);

            return null;
        }

        var size = new SIZE { cx = 256, cy = 256 };

        if (imageFactory.GetImage(size, SIIGBF.SIIGBF_BIGGERSIZEOK, out var bitmapHandle).Failed)
        {
            Marshal.ReleaseComObject(shellItem);

            return null;
        }

        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(bitmapHandle.DangerousGetHandle(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                          .AsFreeze();
        }
        catch
        {
            // ignored
            return null;
        }
        finally
        {
            bitmapHandle.Close();

            Marshal.ReleaseComObject(shellItem);
        }
    }
}
