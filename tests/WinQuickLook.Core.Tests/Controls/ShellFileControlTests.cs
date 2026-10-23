using System;
using System.IO;

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
