using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

using Xunit;

namespace WinQuickLook.Tests.Interop;

public class FriendlyOverloadExtensionsTests
{
    [Theory]
    [InlineData(0x12345678)]
    [InlineData(-123456)]
    public void FindWindowSW_ReusedOutput_ReplacesAllBits(int nativeHwnd)
    {
        IShellWindows windows = new ShellWindowsStub { NativeHwnd = nativeHwnd };
        var hwnd = new HWND(unchecked((nint)0x1122334400000000L));
        object empty = null!;

        var hr = windows.FindWindowSW(empty, empty, 0, out hwnd, 0, out object? dispatch);

        Assert.Equal(0, hr.Value);
        Assert.Equal((nint)nativeHwnd, (nint)hwnd);
        Assert.Same(windows, dispatch);
    }

    [Fact]
    public void Item_WrongInterface_ReturnsNoInterface()
    {
        IShellWindows windows = new ShellWindowsStub { ItemResult = default, ItemValue = new object() };

        var hr = windows.Item(0, out IWebBrowserApp? browser);

        Assert.Equal(unchecked((int)0x80004002), hr.Value);
        Assert.Null(browser);
    }

    [Fact]
    public void Item_MissingWindow_PreservesFalseResult()
    {
        IShellWindows windows = new ShellWindowsStub();

        var hr = windows.Item(0, out IWebBrowserApp? browser);

        Assert.Equal(1, hr.Value);
        Assert.Null(browser);
    }
}
