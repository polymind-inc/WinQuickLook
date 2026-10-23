using System.IO;

using Windows.Win32;

using Xunit;

namespace WinQuickLook.Tests.Interop;

public class ComInteropStreamTests
{
    [Fact]
    public unsafe void Read_NullByteCount_ReadsDataAndAdvancesStream()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var adapter = new ComInteropStream(stream);
        byte* buffer = stackalloc byte[2];

        var result = adapter.Read(buffer, 2, null);

        Assert.True(result.Succeeded);
        Assert.Equal(1, buffer[0]);
        Assert.Equal(2, buffer[1]);
        Assert.Equal(2, stream.Position);
    }

    [Fact]
    public unsafe void Read_ByteCount_ReportsActualBytesRead()
    {
        using var stream = new MemoryStream([1]);
        var adapter = new ComInteropStream(stream);
        byte* buffer = stackalloc byte[4];
        uint bytesRead;

        adapter.Read(buffer, 4, &bytesRead);

        Assert.Equal(1u, bytesRead);
        Assert.Equal(1, buffer[0]);
    }

    [Fact]
    public unsafe void Read_ZeroBytesWithNullPointers_DoesNotAdvanceStream()
    {
        using var stream = new MemoryStream([1]);
        var adapter = new ComInteropStream(stream);

        var result = adapter.Read(null, 0, null);

        Assert.True(result.Succeeded);
        Assert.Equal(0, stream.Position);
    }
}
