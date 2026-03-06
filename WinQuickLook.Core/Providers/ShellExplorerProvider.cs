using System;
using System.IO;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

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

        // ReSharper disable once SuspiciousTypeConversion.Global
        var shellWindows = (IShellWindows)new ShellWindows();

        var fileSystemInfo = default(FileSystemInfo);

        if (IsDesktopWindow(foregroundHwnd))
        {
            var pvarLoc = new object();

            shellWindows.FindWindowSW(pvarLoc, pvarLoc, (int)ShellWindowTypeConstants.SWC_DESKTOP, out var desktopHwnd, (int)ShellWindowFindWindowOptions.SWFO_NEEDDISPATCH, out IWebBrowserApp webBrowserApp);

            if (!IsCaretActive(desktopHwnd))
            {
                fileSystemInfo = GetSelectedItemCore(webBrowserApp);
            }

            Marshal.ReleaseComObject(webBrowserApp);
        }
        else
        {
            shellWindows.get_Count(out var count);

            for (var i = 0; i < count; i++)
            {
                shellWindows.Item(i, out IWebBrowserApp webBrowserApp);

                try
                {
                    webBrowserApp.get_HWND(out var hwnd);

                    if (hwnd != foregroundHwnd || IsCaretActive(hwnd))
                    {
                        continue;
                    }

                    fileSystemInfo = GetSelectedItemCore(webBrowserApp);

                    break;
                }
                finally
                {
                    Marshal.ReleaseComObject(webBrowserApp);
                }
            }
        }

        Marshal.ReleaseComObject(shellWindows);

        return fileSystemInfo;
    }

    private FileSystemInfo? GetSelectedItemCore(IWebBrowserApp webBrowserApp)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        var serviceProvider = (IServiceProvider)webBrowserApp;

        serviceProvider.QueryService(PInvoke.SID_STopLevelBrowser, out IShellBrowser shellBrowser);

        shellBrowser.QueryActiveShellView(out var shellView);

        // ReSharper disable once SuspiciousTypeConversion.Global
        var folderView = (IFolderView)shellView;

        folderView.GetFocusedItem(out var index);
        folderView.Item(index, out nint ppidl);

        folderView.GetFolder(out IPersistFolder2 persistFolder2);

        // ReSharper disable once SuspiciousTypeConversion.Global
        var shellFolder = (IShellFolder)persistFolder2;

        shellFolder.GetDisplayNameOf(ppidl, SHGDNF.SHGDN_FORPARSING, out var pName);

        Span<char> pszBuf = new char[PInvoke.MAX_PATH];

        PInvoke.StrRetToBuf(ref pName, ppidl, pszBuf);

        try
        {
            var selectedPath = new string(pszBuf.TrimEnd('\0'));

            if (File.Exists(selectedPath))
            {
                return new FileInfo(selectedPath);
            }

            if (Directory.Exists(selectedPath))
            {
                return new DirectoryInfo(selectedPath);
            }

            return null;
        }
        finally
        {
            Marshal.FreeCoTaskMem(ppidl);

            Marshal.ReleaseComObject(shellFolder);
            Marshal.ReleaseComObject(persistFolder2);
            Marshal.ReleaseComObject(folderView);
            Marshal.ReleaseComObject(shellView);
            Marshal.ReleaseComObject(shellBrowser);
            Marshal.ReleaseComObject(serviceProvider);
        }
    }

    private static bool IsCaretActive(HWND hwnd)
    {
        var threadId = PInvoke.GetWindowThreadProcessId(hwnd);

        var info = new GUITHREADINFO
        {
            cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>()
        };

        PInvoke.GetGUIThreadInfo(threadId, ref info);

        return info.flags != 0 || info.hwndCaret != nint.Zero;
    }

    private static bool IsDesktopWindow(HWND hwnd)
    {
        Span<char> classNameBuf = new char[64];

        PInvoke.GetClassName(hwnd, classNameBuf);

        var className = new string(classNameBuf);

        if (className != "Progman" && className != "WorkerW")
        {
            return false;
        }

        return PInvoke.FindWindowEx(hwnd, new HWND(), "SHELLDLL_DefView", null) != nint.Zero;
    }
}
