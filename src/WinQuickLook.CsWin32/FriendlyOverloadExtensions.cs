using System.Runtime.InteropServices;

using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Windows.Win32;

public static class FriendlyOverloadExtensions
{
    public static unsafe void FreeCoTaskMem(this PWSTR value) => Marshal.FreeCoTaskMem((nint)value.Value);

    extension(IFolderView folderView)
    {
        public unsafe HRESULT Item(int iItemIndex, out nint ppidl)
        {
            ppidl = default;

            fixed (nint* ppidlLocal = &ppidl)
            {
                return folderView.Item(iItemIndex, (UI.Shell.Common.ITEMIDLIST**)ppidlLocal);
            }
        }
    }

    public static unsafe HRESULT GetDisplayNameOf(this IShellFolder folderView, in nint pidl, SHGDNF uFlags, out UI.Shell.Common.STRRET pName)
    {
        pName = default;

        fixed (UI.Shell.Common.STRRET* pNameLocal = &pName)
        {
            return folderView.GetDisplayNameOf((UI.Shell.Common.ITEMIDLIST*)pidl, uFlags, pNameLocal);
        }
    }

    public static unsafe HRESULT get_Count(this IShellWindows shellWindows, out int count)
    {
        fixed (int* countLocal = &count)
        {
            return shellWindows.get_Count(countLocal);
        }
    }

    public static unsafe HRESULT get_HWND(this IWebBrowserApp webBrowserApp, out HWND hWnd)
    {
        fixed (HWND* hWndLocal = &hWnd)
        {
            return webBrowserApp.get_HWND((SHANDLE_PTR*)hWndLocal);
        }
    }

    extension(IShellWindows shellWindows)
    {
        public HRESULT Item<T>(object index, out T? folder) where T : class
        {
            var hr = shellWindows.Item(index, out var o);

            return GetInterface(hr, o, out folder);
        }

        // ReSharper disable once InconsistentNaming
        public unsafe HRESULT FindWindowSW<T>(in object pvarLoc, in object pvarLocRoot, int swClass, out HWND phwnd, int swfwOptions, out T? ppdispOut) where T : class
        {
            var nativeHwnd = 0;
            var hr = shellWindows.FindWindowSW(pvarLoc, pvarLocRoot, swClass, &nativeHwnd, swfwOptions, out var o);

            phwnd = new HWND(nativeHwnd);

            return GetInterface(hr, o, out ppdispOut);
        }
    }

    private static HRESULT GetInterface<T>(HRESULT hr, object? value, out T? result) where T : class
    {
        if (hr.Value == 0 && value is T typedValue)
        {
            result = typedValue;

            return hr;
        }

        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }

        result = null;

        return hr.Value == 0 ? new HRESULT(unchecked((int)0x80004002)) : hr;
    }
}
