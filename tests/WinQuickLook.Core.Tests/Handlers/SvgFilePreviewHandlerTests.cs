using System;
using System.IO;

using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class SvgFilePreviewHandlerTests
{
    [Fact]
    public void TryCreateViewer_SupportedSvgExtension_ThrowsNotImplementedException()
    {
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test.svg"));

        var handler = new SvgFilePreviewHandler();

        Assert.Throws<NotImplementedException>(() => handler.TryCreateViewer(fileInfo, out _));
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
