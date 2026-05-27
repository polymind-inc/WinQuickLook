using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinQuickLook.App.WinUI;

internal sealed class NativeNotifyIcon : IDisposable
{
    private const uint CallbackMessage = 0x8000 + 1;
    private const uint IconId = 1;
    private const uint PreviewCommandId = 100;
    private const uint ExitCommandId = 101;

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIM_SETVERSION = 0x00000004;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint NOTIFYICON_VERSION_4 = 4;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONUP = 0x0205;
    private const int GWLP_WNDPROC = -4;
    private const uint MF_STRING = 0x00000000;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint TPM_RETURNCMD = 0x00000100;
    private const uint TPM_RIGHTBUTTON = 0x00000002;
    private const int SW_HIDE = 0;

    private readonly nint _hwnd;
    private readonly Action _previewSelected;
    private readonly Action _exitSelected;
    private readonly WndProc _wndProc;
    private readonly nint _oldWndProc;
    private readonly nint _icon;
    private readonly bool _destroyIcon;

    private bool _disposed;

    public NativeNotifyIcon(nint hwnd, string tooltip, Action previewSelected, Action exitSelected)
    {
        _hwnd = hwnd;
        _previewSelected = previewSelected;
        _exitSelected = exitSelected;
        _wndProc = WndProcCore;
        _oldWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProc));
        _icon = ExtractIcon(nint.Zero, Environment.ProcessPath!, 0);

        if (_icon == nint.Zero)
        {
            _icon = LoadIcon(nint.Zero, new IntPtr(32512));
        }
        else
        {
            _destroyIcon = true;
        }

        var data = CreateNotifyIconData(tooltip);

        if (!Shell_NotifyIcon(NIM_ADD, ref data))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        data.uVersion = NOTIFYICON_VERSION_4;
        Shell_NotifyIcon(NIM_SETVERSION, ref data);
    }

    public static void HideWindow(nint hwnd)
    {
        ShowWindow(hwnd, SW_HIDE);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        var data = CreateNotifyIconData(string.Empty);

        Shell_NotifyIcon(NIM_DELETE, ref data);
        SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _oldWndProc);
        if (_destroyIcon)
        {
            DestroyIcon(_icon);
        }

        _disposed = true;
    }

    private NotifyIconData CreateNotifyIconData(string tooltip)
    {
        return new NotifyIconData
        {
            cbSize = Marshal.SizeOf<NotifyIconData>(),
            hWnd = _hwnd,
            uID = IconId,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = CallbackMessage,
            hIcon = _icon,
            szTip = tooltip,
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };
    }

    private nint WndProcCore(nint hwnd, uint message, nuint wParam, nint lParam)
    {
        if (message == CallbackMessage)
        {
            switch ((uint)lParam.ToInt64() & 0xFFFF)
            {
                case WM_LBUTTONDBLCLK:
                    _previewSelected();
                    return 0;
                case WM_RBUTTONUP:
                    ShowContextMenu();
                    return 0;
            }
        }

        return CallWindowProc(_oldWndProc, hwnd, message, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        if (!GetCursorPos(out var point))
        {
            return;
        }

        var menu = CreatePopupMenu();

        if (menu == nint.Zero)
        {
            return;
        }

        try
        {
            AppendMenu(menu, MF_STRING, PreviewCommandId, "Preview selected");
            AppendMenu(menu, MF_SEPARATOR, 0, null);
            AppendMenu(menu, MF_STRING, ExitCommandId, "Exit");
            SetForegroundWindow(_hwnd);

            var command = TrackPopupMenu(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON, point.X, point.Y, 0, _hwnd, nint.Zero);

            switch (command)
            {
                case PreviewCommandId:
                    _previewSelected();
                    break;
                case ExitCommandId:
                    _exitSelected();
                    break;
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    private static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint msg, nuint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool AppendMenu(nint hMenu, uint uFlags, uint uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint TrackPopupMenu(nint hMenu, uint uFlags, int x, int y, int nReserved, nint hWnd, nint prcRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(nint hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern nint ExtractIcon(nint hInst, string lpszExeFileName, uint nIconIndex);

    [DllImport("user32.dll")]
    private static extern nint LoadIcon(nint hInstance, nint lpIconName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint hIcon);

    private delegate nint WndProc(nint hwnd, uint message, nuint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }
}
