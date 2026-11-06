using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Media.MediaFoundation;

using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class MediaFilePreviewHandlerTests
{
    [Theory]
    [InlineData(0, 1920, 1080)]
    [InlineData(90, 1080, 1920)]
    [InlineData(180, 1920, 1080)]
    [InlineData(270, 1080, 1920)]
    public void TryGetVideoSize_UsesDisplayOrientation(uint rotation, double width, double height)
    {
        Assert.True(PInvoke.MFCreateAttributes(out var attributes, 2).Succeeded);
        try
        {
            Assert.True(attributes.SetUINT64(PInvoke.MF_MT_FRAME_SIZE, (1920ul << 32) | 1080ul).Succeeded);
            Assert.True(attributes.SetUINT32(PInvoke.MF_MT_VIDEO_ROTATION, rotation).Succeeded);

            Assert.True(MediaFilePreviewHandler.TryGetVideoSize(attributes, out var size));
            Assert.Equal(width, size.Width);
            Assert.Equal(height, size.Height);
        }
        finally
        {
            Marshal.ReleaseComObject(attributes);
        }
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    public void TryGetVideoSize_RejectsEmptyFrames(uint width, uint height)
    {
        Assert.True(PInvoke.MFCreateAttributes(out var attributes, 1).Succeeded);
        try
        {
            Assert.True(attributes.SetUINT64(PInvoke.MF_MT_FRAME_SIZE, ((ulong)width << 32) | height).Succeeded);
            Assert.False(MediaFilePreviewHandler.TryGetVideoSize(attributes, out _));
        }
        finally
        {
            Marshal.ReleaseComObject(attributes);
        }
    }
}
