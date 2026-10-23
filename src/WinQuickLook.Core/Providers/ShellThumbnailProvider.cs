using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using Windows.Win32;
using Windows.Win32.Foundation;
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
                using var bitmap = Image.FromHbitmap(bitmapHandle.DangerousGetHandle());
                using var buffer = new MemoryStream();
                bitmap.Save(buffer, ImageFormat.Png);
                return buffer.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
