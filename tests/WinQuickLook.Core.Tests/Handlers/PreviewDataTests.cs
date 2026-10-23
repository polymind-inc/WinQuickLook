using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class PreviewDataTests
{
    [StaFact]
    public void ImagePreview_PreservesEncodedDataAndPixelSizeWithoutLockingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var bitmap = new Bitmap(2400, 1200))
            {
                bitmap.Save(path, ImageFormat.Png);
            }

            var handler = new ImageFilePreviewHandler();
            Assert.True(handler.TryCreateViewer(new FileInfo(path), out var result));
            Assert.NotNull(result);
            Assert.Equal(2400, result.RequestSize.Width);
            Assert.Equal(1200, result.RequestSize.Height);
            Assert.Equal(File.ReadAllBytes(path), Assert.IsType<ImagePreview>(result.Content).Data);
            using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [StaFact]
    public void ImagePreview_InvalidImage_ReturnsFalse()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, [0, 1, 2, 3]);
            Assert.False(new ImageFilePreviewHandler().TryCreateViewer(new FileInfo(path), out var result));
            Assert.Null(result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MarkdownPreview_RendersAdvancedSyntaxAndPreservesSourceWithoutLockingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.MARKDOWN");
        const string source = "# 日本語\n\n| Name | Value |\n|---|---|\n| Test | 1 |\n";
        try
        {
            File.WriteAllText(path, source);
            Assert.True(new MarkdownFilePreviewHandler().TryCreateViewer(new FileInfo(path), out var result));
            Assert.NotNull(result);
            var preview = Assert.IsType<HtmlPreview>(result.Content);
            Assert.Equal(source, preview.Source);
            Assert.Contains("<table>", preview.Html);
            Assert.Contains("日本語", preview.Html);
            using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GenericDirectoryPreview_DoesNotEnumerateDirectoryDuringSelection()
    {
        var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Assert.True(new GenericDirectoryPreviewHandler().TryCreateViewer(directory, out var result));
        Assert.NotNull(result);
        Assert.Same(directory, Assert.IsType<GenericPreview>(result.Content).FileSystemInfo);
    }
}
