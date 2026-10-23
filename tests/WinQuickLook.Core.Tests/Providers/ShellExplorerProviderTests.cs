using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using WinQuickLook.Providers;

using Xunit;

namespace WinQuickLook.Tests.Providers;

public class ShellExplorerProviderTests
{
    [Fact]
    public void IsDesktopWindow_DesktopShellView_ReturnsTrue()
    {
        foreach (var className in new[] { "Progman", "WorkerW" })
        {
            var hwnd = default(HWND);

            while ((hwnd = PInvoke.FindWindowEx(default, hwnd, className, null)) != nint.Zero)
            {
                if (PInvoke.FindWindowEx(hwnd, default, "SHELLDLL_DefView", null) == nint.Zero)
                {
                    continue;
                }

                Assert.True(ShellExplorerProvider.IsDesktopWindow(hwnd));

                return;
            }
        }

        Assert.Skip("The desktop Shell view is not available in this session.");
    }

    [StaFact]
    public void IsDesktopWindow_OtherWindow_ReturnsFalse()
    {
        var hwnd = PInvoke.CreateWindowEx2(0, "static", "", WINDOW_STYLE.WS_POPUP, 0, 0, 0, 0, default, null, null);

        Assert.NotEqual(nint.Zero, (nint)hwnd);

        try
        {
            Assert.False(ShellExplorerProvider.IsDesktopWindow(hwnd));
        }
        finally
        {
            PInvoke.DestroyWindow(hwnd);
        }
    }
}
