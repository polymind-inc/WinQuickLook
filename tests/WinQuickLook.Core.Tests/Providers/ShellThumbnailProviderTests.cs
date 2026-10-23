using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

using WinQuickLook.Providers;

using Xunit;

namespace WinQuickLook.Tests.Providers;

public class ShellThumbnailProviderTests
{
    [Theory]
    [InlineData(24)]
    [InlineData(32)]
    public unsafe void EncodeThumbnail_PreservesColorsOrientationAndAlpha(int bitCount)
    {
        var info = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = 2,
                biHeight = -2,
                biPlanes = 1,
                biBitCount = (ushort)bitCount
            }
        };
        using var bitmap = PInvoke.CreateDIBSection(default, &info, DIB_USAGE.DIB_RGB_COLORS, out var pixels, null, 0);
        Assert.False(bitmap.IsInvalid);

        byte[] data = bitCount == 32
            ? [0, 0, 0, 0, 0, 0, 128, 128, 0, 255, 0, 255, 255, 0, 0, 255]
            : [0, 0, 255, 0, 255, 0, 0, 0, 255, 0, 0, 255, 255, 255, 0, 0];
        data.CopyTo(new Span<byte>(pixels, data.Length));

        var png = Assert.IsType<byte[]>(ShellThumbnailProvider.EncodeThumbnail(bitmap));
        using var stream = new MemoryStream(png);
        using var image = new Bitmap(stream);

        Assert.Equal(2, image.Width);
        Assert.Equal(2, image.Height);
        Assert.Equal(bitCount == 32 ? 0 : 255, image.GetPixel(0, 0).A);
        if (bitCount == 32)
        {
            Assert.Equal(128, image.GetPixel(1, 0).A);
            Assert.Equal(255, image.GetPixel(1, 0).R);
            Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(0, 1).ToArgb());
        }
        else
        {
            Assert.Equal(Color.Red.ToArgb(), image.GetPixel(0, 0).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), image.GetPixel(0, 1).ToArgb());
        }
    }
}
