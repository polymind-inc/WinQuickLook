using System.IO;


using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class FilePreviewHandlerTests
{
    [StaFact]
    public void TryCreateViewer_IgnoredExecutableExtension_ReturnsFalse()
    {
        var handler = new TestFilePreviewHandler(HandlerPriorityClass.Normal);
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "sample.exe"));

        var actual = handler.TryCreateViewer(fileInfo, out var handlerResult);

        Assert.False(actual);
        Assert.Null(handlerResult);
        Assert.False(handler.WasCalled);
    }

    [StaFact]
    public void TryCreateViewer_GenericHandlerWithIgnoredExtension_ReturnsTrue()
    {
        var handler = new TestFilePreviewHandler(HandlerPriorityClass.Generic);
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "sample.exe"));

        var actual = handler.TryCreateViewer(fileInfo, out var handlerResult);

        Assert.True(actual);
        Assert.NotNull(handlerResult);
        Assert.True(handler.WasCalled);
    }

    [StaFact]
    public void TryCreateViewer_DirectoryInput_ReturnsFalse()
    {
        var handler = new TestFilePreviewHandler(HandlerPriorityClass.Normal);
        var directoryInfo = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        var actual = handler.TryCreateViewer(directoryInfo, out var handlerResult);

        Assert.False(actual);
        Assert.Null(handlerResult);
        Assert.False(handler.WasCalled);
    }

    private sealed class TestFilePreviewHandler(HandlerPriorityClass priorityClass) : FilePreviewHandler
    {
        public bool WasCalled { get; private set; }

        public override HandlerPriorityClass PriorityClass => priorityClass;

        protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
        {
            WasCalled = true;
            handlerResult = new HandlerResult { Content = new GenericPreview(fileInfo) };

            return true;
        }
    }
}
