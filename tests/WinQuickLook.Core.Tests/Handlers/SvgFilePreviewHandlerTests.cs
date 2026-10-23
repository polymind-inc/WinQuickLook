using System.IO;

using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class SvgFilePreviewHandlerTests
{
    [Theory]
    [InlineData("test.svg")]
    [InlineData("test.svgz")]
    public void TryCreateViewer_UnimplementedSvgExtension_ReturnsFalse(string fileName)
    {
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), fileName));

        var handler = new SvgFilePreviewHandler();

        var actual = handler.TryCreateViewer(fileInfo, out var handlerResult);

        Assert.False(actual);
        Assert.Null(handlerResult);
    }

    [Fact]
    public void TryCreateViewer_UnsupportedSvgExtension_ReturnsFalse()
    {
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test.jpg"));

        var handler = new SvgFilePreviewHandler();

        var actual = handler.TryCreateViewer(fileInfo, out var handlerResult);

        Assert.False(actual);
        Assert.Null(handlerResult);
    }
}
