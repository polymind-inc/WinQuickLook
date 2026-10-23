using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

using Moq;

using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.PropertiesSystem;

using WinQuickLook.Controls;

using Xunit;

namespace WinQuickLook.Tests.Controls;

public class ShellFileControlTests
{
    [WpfFact]
    public void Unloaded_ReleasesHandlerAndFileHandleImmediately()
    {
        var path = Path.GetTempFileName();
        using var control = new ShellFileControl();
        var handler = CreateStreamHandler();
        var panel = new Grid();
        panel.Children.Add(control);

        try
        {
            using var source = new HwndSource(new HwndSourceParameters("Shell lifetime test") { Width = 10, Height = 10, WindowStyle = 0 });
            source.RootVisual = panel;
            panel.Measure(new Size(10, 10));
            panel.Arrange(new Rect(0, 0, 10, 10));
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

            Assert.NotEqual(nint.Zero, control.Handle);
            Assert.True(control.Open(new FileInfo(path), handler));

            panel.Children.Clear();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

            Assert.Equal(1, handler.UnloadCount);

            using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [WpfFact]
    public void Open_ReplacementAndDispose_ReleaseEachHandlerOnce()
    {
        var path = Path.GetTempFileName();
        using var control = new ShellFileControl();
        var first = CreateStreamHandler();
        var second = CreateStreamHandler();

        try
        {
            Assert.True(control.Open(new FileInfo(path), first));
            Assert.True(control.Open(new FileInfo(path), second));

            Assert.Equal(1, first.UnloadCount);
            Assert.Equal(0, second.UnloadCount);

            control.Dispose();

            Assert.Equal(1, first.UnloadCount);
            Assert.Equal(1, second.UnloadCount);

            using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [WpfFact]
    public void Open_PreviewFailure_ReleasesHandlerAndStream()
    {
        var path = Path.GetTempFileName();
        using var control = new ShellFileControl();
        var handler = CreateStreamHandler();
        handler.PreviewResult = new HRESULT(unchecked((int)0x80004005));

        try
        {
            Assert.False(control.Open(new FileInfo(path), handler));

            Assert.Equal(1, handler.UnloadCount);

            using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static PreviewHandlerStub CreateStreamHandler() => new();

    private unsafe sealed class PreviewHandlerStub : IPreviewHandler, IInitializeWithStream
    {
        public int UnloadCount { get; private set; }
        public HRESULT PreviewResult { get; set; }

        public HRESULT Initialize(IStream stream, uint mode) => default;
        public HRESULT SetWindow(HWND window, RECT* rectangle) => default;
        public HRESULT SetRect(RECT* rectangle) => default;
        public HRESULT DoPreview() => PreviewResult;
        public HRESULT Unload() { UnloadCount++; return default; }
        public HRESULT SetFocus() => default;
        public HRESULT QueryFocus(HWND* window) { *window = default; return default; }
        public HRESULT TranslateAccelerator(Windows.Win32.UI.WindowsAndMessaging.MSG* message) => default;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public unsafe void Initialize_StreamHandler_KeepsStreamOpenUntilUnload(bool initializeSucceeds)
    {
        var path = Path.GetTempFileName();
        Stream? previewStream = null;
        IStream? capturedStream = null;
        var handler = new Mock<IPreviewHandler>();

        handler.As<IInitializeWithStream>()
               .Setup(x => x.Initialize(It.IsAny<IStream>(), 0))
               .Callback<IStream, uint>((stream, _) => capturedStream = stream)
               .Returns(new HRESULT(initializeSucceeds ? 0 : unchecked((int)0x80004005)));

        try
        {
            File.WriteAllBytes(path, [1, 2]);

            var actual = ShellFileControl.TryInitializePreviewHandler(handler.Object, new FileInfo(path), out previewStream);

            Assert.Equal(initializeSucceeds, actual);
            Assert.NotNull(previewStream);
            Assert.NotNull(capturedStream);
            Assert.True(previewStream.CanRead);

            byte value;
            uint bytesRead;

            capturedStream.Read(&value, 1, &bytesRead);

            Assert.Equal(1u, bytesRead);
            Assert.Equal(1, value);

            handler.Setup(x => x.Unload()).Callback(() =>
            {
                Assert.True(previewStream.CanRead);
                Assert.Equal(2, previewStream.ReadByte());
            });

            ShellFileControl.UnloadPreviewHandler(handler.Object, previewStream);

            handler.Verify(x => x.Unload(), Times.Once);
            Assert.False(previewStream.CanRead);

            using var exclusiveStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            previewStream?.Dispose();
            File.Delete(path);
        }
    }

    [Fact]
    public void Initialize_Exception_PreservesStreamForCleanup()
    {
        var path = Path.GetTempFileName();
        Stream? previewStream = null;
        var handler = new Mock<IPreviewHandler>();

        handler.As<IInitializeWithStream>()
               .Setup(x => x.Initialize(It.IsAny<IStream>(), 0))
               .Throws<InvalidOperationException>();

        try
        {
            Assert.Throws<InvalidOperationException>(() => ShellFileControl.TryInitializePreviewHandler(handler.Object, new FileInfo(path), out previewStream));
            Assert.NotNull(previewStream);

            ShellFileControl.UnloadPreviewHandler(handler.Object, previewStream);

            Assert.False(previewStream.CanRead);
        }
        finally
        {
            previewStream?.Dispose();
            File.Delete(path);
        }
    }

    [Fact]
    public void Unload_Exception_DisposesStream()
    {
        using var previewStream = new MemoryStream([1]);
        var handler = new Mock<IPreviewHandler>();

        handler.Setup(x => x.Unload()).Throws<InvalidOperationException>();

        Assert.Throws<InvalidOperationException>(() => ShellFileControl.UnloadPreviewHandler(handler.Object, previewStream));

        Assert.False(previewStream.CanRead);
    }
}
