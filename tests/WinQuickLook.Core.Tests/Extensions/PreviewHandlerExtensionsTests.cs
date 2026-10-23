using System.IO;

using Moq;

using WinQuickLook.Extensions;
using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Extensions;

public class PreviewHandlerExtensionsTests
{
    [Theory]
    [InlineData("test.pdf")]
    [InlineData("test.PDF")]
    [InlineData("test.svg")]
    [InlineData("test.SVG")]
    [InlineData("test.svgz")]
    [InlineData("test.SVGZ")]
    public void TryCreateViewer_UnimplementedFormat_UsesNextHandler(string fileName)
    {
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), fileName));
        var expected = new HandlerResult();
        var fallback = new Mock<IFileSystemPreviewHandler>();

        fallback.Setup(x => x.TryCreateViewer(fileInfo, out expected)).Returns(true);

        IFileSystemPreviewHandler[] handlers = [new PdfFilePreviewHandler(), new SvgFilePreviewHandler(), fallback.Object];

        var actual = handlers.TryCreateViewer(fileInfo, out var handlerResult);

        Assert.True(actual);
        Assert.Same(expected, handlerResult);
        fallback.Verify(x => x.TryCreateViewer(fileInfo, out expected), Times.Once);
    }
}
