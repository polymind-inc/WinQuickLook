using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Windows.Win32;

public static class FriendlyOverloadExtensions
{
    extension(IFolderView folderView)
    {
        public unsafe void Item(int iItemIndex, out nint ppidl)
        {
            fixed (nint* ppidlLocal = &ppidl)
            {
                folderView.Item(iItemIndex, (UI.Shell.Common.ITEMIDLIST**)ppidlLocal);
            }
        }
    }

    public static unsafe void GetDisplayNameOf(this IShellFolder folderView, in nint pidl, SHGDNF uFlags, out UI.Shell.Common.STRRET pName)
    {
        fixed (UI.Shell.Common.STRRET* pNameLocal = &pName)
        {
            folderView.GetDisplayNameOf((UI.Shell.Common.ITEMIDLIST*)pidl, uFlags, pNameLocal);
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
        public HRESULT Item<T>(object index, out T folder)
        {
            var hr = shellWindows.Item(index, out var o);
            folder = (T)o;
            return hr;
        }

        // ReSharper disable once InconsistentNaming
        public unsafe HRESULT FindWindowSW<T>(in object pvarLoc, in object pvarLocRoot, int swClass, out HWND phwnd, int swfwOptions, out T ppdispOut)
        {
            fixed (HWND* phwndLocal = &phwnd)
            {
                var hr = shellWindows.FindWindowSW(pvarLoc, pvarLocRoot, swClass, (int*)phwndLocal, swfwOptions, out var o);
                ppdispOut = (T)o;
                return hr;
            }
        }
    }
}
