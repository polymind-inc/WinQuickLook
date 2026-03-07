using System.IO;

using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class CodeFilePreviewHandlerTests
{
    [WpfTheory]
    [InlineData("test.cs", true)]
    public void TryCreateViewer(string fileName, bool expected)
    {
        var fileInfo = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", fileName));

        var handler = new CodeFilePreviewHandler();

        var actual = handler.TryCreateViewer(fileInfo, out _);

        Assert.Equal(expected, actual);
    }
}
