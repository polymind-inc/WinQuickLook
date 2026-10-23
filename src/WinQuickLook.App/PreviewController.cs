using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;

using WinQuickLook.Extensions;
using WinQuickLook.Handlers;
using WinQuickLook.Providers;

using static Microsoft.UI.Reactor.Factories;

namespace WinQuickLook.App;

public sealed class PreviewController(IEnumerable<IFileSystemPreviewHandler> previewHandlers, ShellAssociationProvider associations)
{
    private readonly IFileSystemPreviewHandler[] _handlers = previewHandlers.OrderByDescending(handler => handler.PriorityClass).ToArray();
    private ReactorWindow? _window;
    private HWND _sourceWindow;

    public void OpenPreview(FileSystemInfo item)
    {
        var sourceWindow = PInvoke.GetForegroundWindow();
        if (!_handlers.TryCreateViewer(item, out var result))
        {
            return;
        }

        var monitor = PInvoke.MonitorFromWindow(sourceWindow, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        PInvoke.GetMonitorInfo(monitor, ref info);
        PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpi, out _);
        var dipScale = Math.Max(dpi, 96) / 96.0;
        var workWidth = (info.rcWork.right - info.rcWork.left) / dipScale;
        var workHeight = (info.rcWork.bottom - info.rcWork.top) / dipScale;
        var scale = Math.Min(Math.Min(workWidth, workHeight) * 0.8 / Math.Max(result.RequestSize.Width, result.RequestSize.Height), 1);
        var width = Math.Max(400, Math.Round(result.RequestSize.Width * scale));
        var height = Math.Max(400, Math.Round(result.RequestSize.Height * scale)
            + AppParameters.CaptionHeight + AppParameters.FooterHeight);
        var file = item as FileInfo;
        var defaultApp = file is not null && associations.TryGetDefault(file, out var entry) ? entry : null;
        var props = new PreviewWindowProps(Guid.NewGuid().ToString("N"), result.Content, defaultApp, associations, ClosePreview, OpenWithAssociation);
        var spec = new WindowSpec
        {
            Title = item.Name,
            Width = width,
            Height = height,
            MinWidth = 400,
            MinHeight = 400,
            ActivateOnOpen = false,
            ShowInTaskbar = false,
            ShowInSwitcher = false,
            IsMinimizable = false,
            IsMaximizable = false,
            Backdrop = BackdropChoice.Of(BackdropKind.Mica),
            CornerStyle = WindowCornerStyle.Rounded,
            Icon = WindowIcon.FromPath(Path.Combine(AppContext.BaseDirectory, "Icon.ico"))
        };
        var visible = _window?.IsVisible == true;
        _sourceWindow = sourceWindow;
        if (_window is null)
        {
            _window = ReactorApp.OpenWindow(spec, _ => Component<PreviewWindow, PreviewWindowProps>(props));
            _window.Closing += (_, e) =>
            {
                e.Cancel = true;
                ClosePreview();
            };
        }
        else
        {
            _window.Update(spec);
            _window.Mount(_ => Component<PreviewWindow, PreviewWindowProps>(props));
        }

        if (!visible)
        {
            var x = info.rcWork.left + ((info.rcWork.right - info.rcWork.left - width * dipScale) / 2);
            var y = info.rcWork.top + ((info.rcWork.bottom - info.rcWork.top - height * dipScale) / 2);
            var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(_window.NativeWindow));
            PInvoke.SetWindowPos(hwnd, default, (int)Math.Round(x), (int)Math.Round(y), 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
            _window.AppWindow.Show(false);
        }

        // Show(false) preserves Explorer focus but can leave the preview behind it.
        // Briefly make it topmost, then restore normal stacking without taking focus.
        var previewHwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(_window.NativeWindow));
        PInvoke.SetWindowPos(previewHwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
        PInvoke.SetWindowPos(previewHwnd, HWND.HWND_NOTOPMOST, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
    }

    public void ClosePreview()
    {
        _window?.Hide();
        _window?.Mount(_ => Empty());
        _sourceWindow = default;
    }

    public void ClosePreviewIfActive()
    {
        if (_window?.IsVisible == true && (_window.IsActive ||
            _sourceWindow != nint.Zero && PInvoke.GetForegroundWindow() == _sourceWindow))
        {
            ClosePreview();
        }
    }

    private void OpenWithAssociation(FileInfo file, ShellAssociationProvider.Entry entry)
    {
        if (entry.IsDefault)
        {
            Process.Start(new ProcessStartInfo(file.FullName) { UseShellExecute = true });
        }
        else
        {
            associations.Invoke(entry.Name, file);
        }
        ClosePreview();
    }
}
