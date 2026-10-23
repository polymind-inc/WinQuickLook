using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Moq;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

using WinQuickLook.Providers;
using WinQuickLook.Tests.Interop;

using Xunit;

namespace WinQuickLook.Tests.Providers;

public class ShellExplorerProviderTests
{
    [Fact]
    public void GetSelectedItem_WindowDisappears_ReturnsNull()
    {
        var windows = new ShellWindowsStub();

        Assert.Null(ShellExplorerProvider.GetSelectedItem(windows, default));
    }

    [Fact]
    public void GetSelectedItem_CountFails_ReturnsNull()
    {
        var windows = new ShellWindowsStub { CountResult = new HRESULT(unchecked((int)0x80004005)) };

        Assert.Null(ShellExplorerProvider.GetSelectedItem(windows, default));
    }

    [Fact]
    public void GetSelectedItem_ViewFails_ReturnsNull()
    {
        var browser = new Mock<IShellBrowser>(MockBehavior.Strict);
        IShellView view = null!;
        browser.Setup(x => x.QueryActiveShellView(out view)).Returns(new HRESULT(unchecked((int)0x80004005)));

        Assert.Null(ShellExplorerProvider.GetSelectedItemCore(browser.Object));
    }

    [Theory]
    [InlineData(unchecked((int)0x80004005), 0)]
    [InlineData(1, -1)]
    [InlineData(0, -1)]
    public void GetSelectedItem_NoFocusedItem_DoesNotRequestPidl(int result, int index)
    {
        var folder = new Mock<IFolderView>(MockBehavior.Strict);
        folder.Setup(x => x.GetFocusedItem(out index)).Returns(new HRESULT(result));

        Assert.Null(ShellExplorerProvider.GetSelectedItem(folder.Object));
    }

    [Fact]
    public void GetFileSystemInfo_LongPath_ReturnsCompletePath()
    {
        var directories = new List<string>();
        var current = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string? filePath = null;

        try
        {
            for (var i = 0; i < 4; i++)
            {
                Directory.CreateDirectory(current);
                directories.Add(current);
                current = Path.Combine(current, new string('a', 70));
            }

            filePath = Path.Combine(directories[^1], "sample.txt");
            File.WriteAllText(filePath, "test");

            Assert.True(filePath.Length > 260);
            var hr = PInvoke.SHCreateItemFromParsingName(filePath, null, out IShellItem item);

            try
            {
                Assert.True(hr.Succeeded, hr.ToString());

                var actual = Assert.IsType<FileInfo>(ShellExplorerProvider.GetFileSystemInfo(item));

                Assert.Equal(filePath, actual.FullName);
            }
            finally
            {
                if (item is not null)
                {
                    Marshal.ReleaseComObject(item);
                }
            }
        }
        finally
        {
            if (filePath is not null)
            {
                File.Delete(filePath);
            }

            for (var i = directories.Count - 1; i >= 0; i--)
            {
                Directory.Delete(directories[i]);
            }
        }
    }

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
