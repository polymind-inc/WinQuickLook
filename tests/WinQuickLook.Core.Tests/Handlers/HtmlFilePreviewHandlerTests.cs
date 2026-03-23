using System.IO;

using WinQuickLook.Controls;
using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class HtmlFilePreviewHandlerTests
{
    [WpfTheory]
    [InlineData("test.htm", true)]
    [InlineData("test.html", true)]
    [InlineData("test.xhtml", true)]
    [InlineData("test.HTM", true)]
    [InlineData("test.HTML", true)]
    [InlineData("test.XHTML", true)]
    [InlineData("test.jpg", false)]
    [InlineData("test.JPG", false)]
    [InlineData("test", false)]
    public void TryCreateViewer_HtmlAndNonHtmlExtensions_ReturnExpectedResult(string fileName, bool expected)
    {
        var fileInfo = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", fileName));

        var handler = new HtmlFilePreviewHandler();

        var actual = handler.TryCreateViewer(fileInfo, out _);

        Assert.Equal(expected, actual);
    }

    [WpfFact]
    public void TryCreateViewer_SupportedHtmlFile_ReturnsHtmlFileControl()
    {
        var fileInfo = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", "test.html"));

        var handler = new HtmlFilePreviewHandler();

        var actual = handler.TryCreateViewer(fileInfo, out var handlerResult);

        Assert.True(actual);
        Assert.NotNull(handlerResult);
        Assert.IsType<HtmlFileControl>(handlerResult.Content);
    }

    [WpfFact]
    public void TryCreateViewer_IgnoredExecutableExtension_ReturnsFalse()
    {
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test.exe"));

        var handler = new HtmlFilePreviewHandler();

        var actual = handler.TryCreateViewer(fileInfo, out var handlerResult);

        Assert.False(actual);
        Assert.Null(handlerResult);
    }
}
