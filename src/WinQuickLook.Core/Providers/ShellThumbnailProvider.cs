using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;

using WinQuickLook.Interop;

namespace WinQuickLook.Providers;

public class ShellThumbnailProvider
{
    public byte[]? GetImage(FileSystemInfo fileSystemInfo)
    {
        var hr = PInvoke.SHCreateItemFromParsingName(fileSystemInfo.FullName, null, out IShellItem? shellItem);
        using var itemScope = new ComScope(shellItem);

        if (hr.Failed || shellItem is not IShellItemImageFactory imageFactory)
        {
            return null;
        }

        if (imageFactory.GetImage(new SIZE { cx = 256, cy = 256 }, SIIGBF.SIIGBF_BIGGERSIZEOK, out var bitmapHandle).Failed)
        {
            return null;
        }

        using (bitmapHandle)
        {
            try
            {
                return EncodeThumbnail(bitmapHandle);
            }
            catch
            {
                return null;
            }
        }
    }

    internal static byte[]? EncodeThumbnail(SafeHandle bitmapHandle)
    {
        if (PInvoke.GetObject(bitmapHandle, out var nativeBitmap) == 0)
        {
            return null;
        }

        var width = nativeBitmap.bmWidth;
        var height = Math.Abs(nativeBitmap.bmHeight);
        var pixels = new byte[checked(width * height * 4)];
        var info = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32
            }
        };
        var dc = PInvoke.GetDC(default);
        try
        {
            if (PInvoke.GetDIBits(dc, bitmapHandle, 0, (uint)height, pixels, ref info, DIB_USAGE.DIB_RGB_COLORS) != height)
            {
                return null;
            }
        }
        finally
        {
            PInvoke.ReleaseDC(default, dc);
        }

        // Shell thumbnails use premultiplied alpha. Image.FromHbitmap drops it.
        // Older, opaque GDI bitmaps have no alpha channel at all.
        if (nativeBitmap.bmBitsPixel < 32)
        {
            for (var i = 3; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
            }
        }

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        try
        {
            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        using var buffer = new MemoryStream();
        bitmap.Save(buffer, ImageFormat.Png);
        return buffer.ToArray();
    }
}
