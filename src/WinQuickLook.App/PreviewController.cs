using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;

using Windows.Foundation;
using Windows.Graphics;

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

public sealed class PreviewController(IEnumerable<IFileSystemPreviewHandler> previewHandlers, ShellAssociationProvider associations) : IDisposable
{
    private readonly IFileSystemPreviewHandler[] _handlers = previewHandlers.OrderByDescending(handler => handler.PriorityClass).ToArray();
    private ReactorWindow? _window;
    private HWND _sourceWindow;
    private PreviewWindowAnimator? _animator;

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
        var pixelSize = result.Content switch
        {
            ImagePreview image => (Size?)image.PixelSize,
            MediaPreview { IsVideo: true } video => video.PixelSize,
            _ => null
        };
        var padding = result.Content is ImagePreview ? AppParameters.ImagePadding * 2 : 0;
        var minimum = pixelSize is not null ? new Size(360, 180) : new Size(400, 400);
        var clientSize = PreviewSizing.GetClientSize(pixelSize ?? result.RequestSize,
            new Size(workWidth, workHeight),
            new Size(padding, AppParameters.CaptionHeight + AppParameters.FooterHeight + AppParameters.ContentBorderHeight + padding),
            minimum, pixelSize is not null ? dipScale : 1);
        var width = clientSize.Width;
        var height = clientSize.Height;
        var file = item as FileInfo;
        var defaultApp = file is not null && associations.TryGetDefault(file, out var entry) ? entry : null;
        var props = new PreviewWindowProps(Guid.NewGuid().ToString("N"), result.Content, defaultApp, associations, ClosePreview, OpenWithAssociation);
        var spec = new WindowSpec
        {
            Title = item.Name,
            Width = width,
            Height = height,
            MinWidth = Math.Min(minimum.Width, width),
            MinHeight = Math.Min(minimum.Height, height),
            ActivateOnOpen = false,
            ExtendsContentIntoTitleBar = true,
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
            _animator = new PreviewWindowAnimator(_window);
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

        var appWindow = _window.AppWindow;
        var previewHwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(_window.NativeWindow));
        var sameMonitor = PInvoke.MonitorFromWindow(previewHwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY) == monitor;
        if (!visible || !sameMonitor)
        {
            _animator?.Stop();
            // Establish the destination DPI before sizing the client area.
            _window.SetSize(width, height);
            appWindow.Move(new PointInt32(info.rcWork.left, info.rcWork.top));
            appWindow.ResizeClient(new SizeInt32((int)Math.Ceiling(width * dipScale), (int)Math.Ceiling(height * dipScale)));
        }

        var frameWidth = appWindow.Size.Width - appWindow.ClientSize.Width;
        var frameHeight = appWindow.Size.Height - appWindow.ClientSize.Height;
        var targetWidth = (int)Math.Ceiling(width * dipScale) + frameWidth;
        var targetHeight = (int)Math.Ceiling(height * dipScale) + frameHeight;
        var centerX = visible && sameMonitor ? appWindow.Position.X + appWindow.Size.Width / 2.0
            : (info.rcWork.left + info.rcWork.right) / 2.0;
        var centerY = visible && sameMonitor ? appWindow.Position.Y + appWindow.Size.Height / 2.0
            : (info.rcWork.top + info.rcWork.bottom) / 2.0;
        var x = Math.Clamp((int)Math.Round(centerX - targetWidth / 2.0), info.rcWork.left, info.rcWork.right - targetWidth);
        var y = Math.Clamp((int)Math.Round(centerY - targetHeight / 2.0), info.rcWork.top, info.rcWork.bottom - targetHeight);
        _animator?.Resize(new RectInt32(x, y, targetWidth, targetHeight), visible && sameMonitor);
        if (!visible)
        {
            appWindow.Show(false);
        }

        // Show(false) preserves Explorer focus but can leave the preview behind it.
        // Briefly make it topmost, then restore normal stacking without taking focus.
        PInvoke.SetWindowPos(previewHwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
        PInvoke.SetWindowPos(previewHwnd, HWND.HWND_NOTOPMOST, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
    }

    public void ClosePreview()
    {
        _animator?.Stop();
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

    public void Dispose() => _animator?.Dispose();
}
