using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class ImageFilePreviewHandlerTests
{
    [Theory]
    [InlineData(64, 48)]
    [InlineData(2400, 1600)]
    public void TryCreateViewer_PreservesOriginalPixelSize(int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        try
        {
            using (var bitmap = new Bitmap(width, height))
            {
                bitmap.Save(path, ImageFormat.Png);
            }

            var handler = new ImageFilePreviewHandler();
            Assert.True(handler.TryCreateViewer(new FileInfo(path), out var result));
            Assert.NotNull(result);
            var content = Assert.IsType<ImagePreview>(result.Content);
            Assert.Equal(width, content.PixelSize.Width);
            Assert.Equal(height, content.PixelSize.Height);
            Assert.Equal(content.PixelSize, result.RequestSize);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
