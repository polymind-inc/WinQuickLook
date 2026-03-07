using System;
using System.Windows;
using System.Windows.Interop;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinQuickLook.Behaviors;

public class WindowingBehavior
{
    // ReSharper disable once UnusedMember.Global
    public static bool GetPreventClosing(DependencyObject obj) => (bool)obj.GetValue(PreventClosingProperty);

    public static void SetPreventClosing(DependencyObject obj, bool value) => obj.SetValue(PreventClosingProperty, value);

    // ReSharper disable once UnusedMember.Global
    public static bool GetUseSystemBackdrop(DependencyObject obj) => (bool)obj.GetValue(UseSystemBackdropProperty);

    public static void SetUseSystemBackdrop(DependencyObject obj, bool value) => obj.SetValue(UseSystemBackdropProperty, value);

    public static readonly DependencyProperty PreventClosingProperty =
        DependencyProperty.RegisterAttached("PreventClosing", typeof(bool), typeof(Window), new PropertyMetadata(false, PreventClosing_PropertyChangedCallback));

    public static readonly DependencyProperty UseSystemBackdropProperty =
        DependencyProperty.RegisterAttached("UseSystemBackdrop", typeof(bool), typeof(Window), new PropertyMetadata(false, UseSystemBackdrop_PropertyChangedCallback));

    private static void UseSystemBackdrop_PropertyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var window = (Window)obj;

        if ((bool)e.NewValue)
        {
            window.SourceInitialized += SourceInitialized;
        }
        else
        {
            window.SourceInitialized -= SourceInitialized;
        }

        return;

        static void SourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new HWND(new WindowInteropHelper((Window)sender!).Handle);
            var flags = 0x02;

            PInvoke.DwmSetWindowAttribute(hwnd, (DWMWINDOWATTRIBUTE)38, ref flags);
        }
    }

    private static void PreventClosing_PropertyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var window = (Window)obj;

        if ((bool)e.NewValue)
        {
            window.SourceInitialized += SourceInitialized;
            window.Loaded += Loaded;
        }
        else
        {
            window.SourceInitialized -= SourceInitialized;
            window.Loaded -= Loaded;
        }

        return;

        static void SourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new HWND(new WindowInteropHelper((Window)sender!).Handle);

            var style = PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, style & ~(int)(WINDOW_STYLE.WS_SYSMENU | WINDOW_STYLE.WS_MINIMIZEBOX | WINDOW_STYLE.WS_MAXIMIZEBOX));
        }

        static void Loaded(object sender, RoutedEventArgs e)
        {
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper((Window)sender).Handle);

            hwndSource?.AddHook(WndProc);
        }
    }

    private static nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        handled = (uint)msg switch
        {
            PInvoke.WM_SYSKEYDOWN when (VIRTUAL_KEY)wParam == VIRTUAL_KEY.VK_F4 => true,
            PInvoke.WM_NCRBUTTONUP when (uint)wParam == PInvoke.HTCAPTION => true,
            _ => handled
        };

        return nint.Zero;
    }
}
