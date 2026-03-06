using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

using Cylinder;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;

using WinQuickLook.Extensions;
using WinQuickLook.Handlers;
using WinQuickLook.Providers;

namespace WinQuickLook.App;

public partial class MainWindow
{
    public MainWindow(IEnumerable<IFileSystemPreviewHandler> previewHandlers, ShellAssociationProvider shellAssociationProvider)
    {
        InitializeComponent();

        _previewHandlers = previewHandlers.OrderByDescending(x => x.PriorityClass).ToArray();
        _shellAssociationProvider = shellAssociationProvider;
    }

    private readonly IEnumerable<IFileSystemPreviewHandler> _previewHandlers;
    private readonly ShellAssociationProvider _shellAssociationProvider;

    public Ref<FileInfo> FileInfo { get; } = new(null);

    public Ref<ShellAssociationProvider.Entry> Default { get; } = new(null);

    public Ref<IReadOnlyList<ShellAssociationProvider.Entry>> Recommends { get; } = new([]);

    public void OpenPreview(FileSystemInfo fileSystemInfo)
    {
        if (!_previewHandlers.TryCreateViewer(fileSystemInfo, out var handlerResult))
        {
            return;
        }

        var monitorContext = GetCurrentMonitorInfo();

        ApplyRequestSize(handlerResult.RequestSize, monitorContext.Monitor);

        Title = fileSystemInfo.Name;
        AppContentPresenter.Content = handlerResult.Content;

        if (fileSystemInfo is FileInfo fileInfo)
        {
            FileInfo.Value = fileInfo;

            Default.Value = _shellAssociationProvider.TryGetDefault(fileInfo, out var entry) ? entry : null;
        }
        else
        {
            FileInfo.Value = null;
            Default.Value = null;
        }

        Recommends.Value = [];

        if (IsVisible)
        {
            return;
        }

        MoveToCenter(monitorContext);
        Show();
    }

    public void OpenAssociateMenu(Button button)
    {
        if (FileInfo.Value is not null && Recommends.Value?.Count == 0)
        {
            Recommends.Value = _shellAssociationProvider.GetRecommends(FileInfo.Value);
        }

        var contextMenu = (ContextMenu)button.FindResource("AssociateMenu");

        contextMenu.PlacementTarget = button;
        contextMenu.IsOpen = true;
    }

    public void OpenWithAssociation(ShellAssociationProvider.Entry entry)
    {
        if (FileInfo.Value is null)
        {
            return;
        }

        if (entry.IsDefault)
        {
            Process.Start(new ProcessStartInfo(FileInfo.Value.FullName) { UseShellExecute = true });
        }
        else
        {
            _shellAssociationProvider.Invoke(entry.Name, FileInfo.Value);
        }

        ClosePreview();
    }

    public void ClosePreview()
    {
        Hide();

        AppContentPresenter.Content = null;
    }

    public void ClosePreviewIfActive()
    {
        if (IsActive)
        {
            ClosePreview();
        }
    }

    private void ApplyRequestSize(Size requestSize, Rect monitor)
    {
        var minWidthOrHeight = Math.Min(monitor.Width, monitor.Height) * 0.8;
        var scaleFactor = Math.Min(minWidthOrHeight / Math.Max(requestSize.Width, requestSize.Height), 1.0);

        Width = Math.Max(Math.Round(requestSize.Width * scaleFactor), MinWidth);
        Height = Math.Max(Math.Round(requestSize.Height * scaleFactor) + AppParameters.CaptionHeight, MinHeight);
    }

    private void MoveToCenter(MonitorContext monitorContext)
    {
        var dpiFactor = monitorContext.Dpi / 96.0;

        var x = monitorContext.Monitor.X + ((monitorContext.Monitor.Width - (Width * dpiFactor)) / 2);
        var y = monitorContext.Monitor.Y + ((monitorContext.Monitor.Height - (Height * dpiFactor)) / 2);

        var hwnd = new WindowInteropHelper(this).Handle;

        PInvoke.SetWindowPos(new HWND(hwnd), new HWND(), (int)Math.Round(x), (int)Math.Round(y), 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
    }

    private static MonitorContext GetCurrentMonitorInfo()
    {
        var foregroundHwnd = PInvoke.GetForegroundWindow();

        var hMonitor = PInvoke.MonitorFromWindow(foregroundHwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);

        var monitorInfo = new MONITORINFO
        {
            cbSize = (uint)Marshal.SizeOf<MONITORINFO>()
        };

        PInvoke.GetMonitorInfo(hMonitor, ref monitorInfo);
        PInvoke.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _);

        var leftTop = new Point(monitorInfo.rcMonitor.left, monitorInfo.rcMonitor.top);
        var rightBottom = new Point(monitorInfo.rcMonitor.right, monitorInfo.rcMonitor.bottom);

        return new MonitorContext(new Rect(leftTop, rightBottom), dpiX);
    }

    private readonly record struct MonitorContext(Rect Monitor, double Dpi);
}
