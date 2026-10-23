using System;
using System.IO;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

using WinQuickLook.Extensions;
using WinQuickLook.Interop;

using IServiceProvider = Windows.Win32.System.Com.IServiceProvider;

namespace WinQuickLook.Providers;

public class ShellExplorerProvider
{
    public FileSystemInfo? GetSelectedItem()
    {
        var foregroundHwnd = PInvoke.GetForegroundWindow();

        if (foregroundHwnd == nint.Zero)
        {
            return null;
        }

        try
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var shellWindows = (IShellWindows)new ShellWindows();

            using var shellWindowsScope = new ComScope(shellWindows);

            return GetSelectedItem(shellWindows, foregroundHwnd);
        }
        catch (COMException)
        {
            return null;
        }
    }

    internal static FileSystemInfo? GetSelectedItem(IShellWindows shellWindows, HWND foregroundHwnd)
    {
        if (IsDesktopWindow(foregroundHwnd))
        {
            object empty = null!;
            var hr = shellWindows.FindWindowSW(empty, empty, (int)ShellWindowTypeConstants.SWC_DESKTOP, out var desktopHwnd, (int)ShellWindowFindWindowOptions.SWFO_NEEDDISPATCH, out IWebBrowserApp? webBrowserApp);

            using var browserScope = new ComScope(webBrowserApp);

            return hr.Value == 0 && webBrowserApp is not null && !IsCaretActive(desktopHwnd) ? GetSelectedItemCore(webBrowserApp) : null;
        }

        if (shellWindows.get_Count(out var count).Failed)
        {
            return null;
        }

        for (var i = 0; i < count; i++)
        {
            var hr = shellWindows.Item(i, out IWebBrowserApp? webBrowserApp);

            using var browserScope = new ComScope(webBrowserApp);

            if (hr.Value != 0 || webBrowserApp is null || webBrowserApp.get_HWND(out var hwnd).Failed)
            {
                continue;
            }

            if (hwnd == foregroundHwnd && !IsCaretActive(hwnd))
            {
                return GetSelectedItemCore(webBrowserApp);
            }
        }

        return null;
    }

    private static FileSystemInfo? GetSelectedItemCore(IWebBrowserApp webBrowserApp)
    {
        // A cast borrows the existing RCW; only the acquired shellBrowser is owned here.
        if (webBrowserApp is not IServiceProvider serviceProvider)
        {
            return null;
        }

        var hr = serviceProvider.QueryService(PInvoke.SID_STopLevelBrowser, out IShellBrowser shellBrowser);

        using var browserScope = new ComScope(shellBrowser);

        return hr.Succeeded && shellBrowser is not null ? GetSelectedItemCore(shellBrowser) : null;
    }

    internal static FileSystemInfo? GetSelectedItemCore(IShellBrowser shellBrowser)
    {
        var hr = shellBrowser.QueryActiveShellView(out var shellView);

        using var viewScope = new ComScope(shellView);

        return hr.Succeeded && shellView is IFolderView folderView ? GetSelectedItem(folderView) : null;
    }

    internal static FileSystemInfo? GetSelectedItem(IFolderView folderView)
    {
        if (folderView.GetFocusedItem(out var index).Value != 0 || index < 0)
        {
            return null;
        }

        var itemHr = folderView.Item(index, out nint pidl);

        try
        {
            if (itemHr.Failed || pidl == 0)
            {
                return null;
            }

            var folderHr = folderView.GetFolder(out IShellFolder shellFolder);

            using var folderScope = new ComScope(shellFolder);

            if (folderHr.Failed || shellFolder is null)
            {
                return null;
            }

            var hr = PInvoke.SHCreateItemWithParent(shellFolder, pidl, out var shellItem);

            using var itemScope = new ComScope(shellItem);

            return hr.Succeeded && shellItem is not null ? GetFileSystemInfo(shellItem) : null;
        }
        finally
        {
            Marshal.FreeCoTaskMem(pidl);
        }
    }

    internal static FileSystemInfo? GetFileSystemInfo(IShellItem shellItem)
    {
        var hr = shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var name);
        var path = name.ToStringAndFree();

        if (hr.Failed || path is null)
        {
            return null;
        }

        if (File.Exists(path))
        {
            return new FileInfo(path);
        }

        return Directory.Exists(path) ? new DirectoryInfo(path) : null;
    }

    private static bool IsCaretActive(HWND hwnd)
    {
        var threadId = PInvoke.GetWindowThreadProcessId(hwnd);

        if (threadId == 0)
        {
            return true;
        }

        var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };

        return !PInvoke.GetGUIThreadInfo(threadId, ref info) || info.flags != 0 || info.hwndCaret != nint.Zero;
    }

    internal static bool IsDesktopWindow(HWND hwnd)
    {
        Span<char> classNameBuf = new char[64];
        var classNameLength = PInvoke.GetClassName(hwnd, classNameBuf);
        var className = new string(classNameBuf[..classNameLength]);

        return (className == "Progman" || className == "WorkerW") && PInvoke.FindWindowEx(hwnd, default, "SHELLDLL_DefView", null) != nint.Zero;
    }
}
