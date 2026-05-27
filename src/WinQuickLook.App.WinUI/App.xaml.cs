using System;
using System.IO;
using System.Threading;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using Windows.Win32.UI.Input.KeyboardAndMouse;

using WinQuickLook.Messaging;
using WinQuickLook.Preview;
using WinQuickLook.Providers;

using WinRT.Interop;

namespace WinQuickLook.App.WinUI;

public partial class App
{
    private readonly Mutex _mutex = new(false, AppParameters.Title);
    private readonly LowLevelKeyboardHook _keyboardHook = new();
    private readonly ShellExplorerProvider _shellExplorerProvider = new();
    private readonly IPreviewProvider _previewProvider = AggregatePreviewProvider.CreateDefault();
    private readonly DispatcherQueue _dispatcherQueue;

    private Window? _messageWindow;
    private PreviewWindow? _previewWindow;
    private NativeNotifyIcon? _notifyIcon;
    private bool _mutexAcquired;
    private bool _disposed;

    public App()
    {
        InitializeComponent();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!_mutex.WaitOne(0, false))
        {
            Exit();

            return;
        }

        _mutexAcquired = true;

        ConfigureNotifyIcon();
        ConfigureKeyboardHook();
    }

    private void ConfigureNotifyIcon()
    {
        _messageWindow = new Window();
        _messageWindow.Activate();
        NativeNotifyIcon.HideWindow(WindowNative.GetWindowHandle(_messageWindow));

        _notifyIcon = new NativeNotifyIcon(
            WindowNative.GetWindowHandle(_messageWindow),
            AppParameters.Title,
            () => _dispatcherQueue.TryEnqueue(PerformPreview),
            () => _dispatcherQueue.TryEnqueue(ExitApplication));
    }

    private void ConfigureKeyboardHook()
    {
        _keyboardHook.PerformKeyDown = vkCode =>
        {
            switch (vkCode)
            {
                case VIRTUAL_KEY.VK_SPACE:
                    _dispatcherQueue.TryEnqueue(PerformPreview);
                    break;
                case VIRTUAL_KEY.VK_ESCAPE:
                    _dispatcherQueue.TryEnqueue(ClosePreview);
                    break;
            }
        };

        _keyboardHook.Start();
    }

    private void PerformPreview()
    {
        if (_previewWindow is not null)
        {
            ClosePreview();

            return;
        }

        var fileSystemInfo = _shellExplorerProvider.GetSelectedItem();

        if (fileSystemInfo is null || !_previewProvider.CanPreview(fileSystemInfo))
        {
            return;
        }

        var request = fileSystemInfo is FileInfo fileInfo
            ? PreviewRequestFactory.CreateWithDirectorySiblings(fileInfo.FullName, _previewProvider)
            : PreviewRequestFactory.Create(fileSystemInfo.FullName);

        _previewWindow = new PreviewWindow(_previewProvider, request);
        _previewWindow.Closed += (_, _) => _previewWindow = null;
        _previewWindow.Activate();
    }

    private void ClosePreview()
    {
        var previewWindow = _previewWindow;

        if (previewWindow is null)
        {
            return;
        }

        _previewWindow = null;
        previewWindow.Close();
    }

    private void ExitApplication()
    {
        DisposeServices();
        Exit();
    }

    private void DisposeServices()
    {
        if (_disposed)
        {
            return;
        }

        ClosePreview();
        _keyboardHook.Dispose();

        if (_notifyIcon is not null)
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        if (_mutexAcquired)
        {
            _mutex.ReleaseMutex();
            _mutexAcquired = false;
        }

        _mutex.Dispose();
        _disposed = true;
    }
}
