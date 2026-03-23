using System.IO;

using WinQuickLook.Controls;
using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class GenericFilePreviewHandlerTests
{
    [WpfFact]
    public void TryCreateViewer_AnyFile_ReturnsGenericFileControl()
    {
        var fileInfo = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", "test.cs"));

        var handler = new GenericFilePreviewHandler();

        var actual = handler.TryCreateViewer(fileInfo, out var handlerResult);

        Assert.True(actual);
        Assert.NotNull(handlerResult);
        Assert.IsType<GenericFileControl>(handlerResult.Content);
    }

    [WpfFact]
    public void TryCreateViewer_AnyFile_ReturnsExpectedRequestSize()
    {
        var fileInfo = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", "test.cs"));

        var handler = new GenericFilePreviewHandler();

        var actual = handler.TryCreateViewer(fileInfo, out var handlerResult);

        Assert.True(actual);
        Assert.NotNull(handlerResult);
        Assert.Equal(572, handlerResult.RequestSize.Width);
        Assert.Equal(290, handlerResult.RequestSize.Height);
    }
}
