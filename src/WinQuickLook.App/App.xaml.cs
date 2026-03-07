using System.Threading;
using System.Windows;
using System.Windows.Controls;

using Hardcodet.Wpf.TaskbarNotification;

using Windows.Win32.UI.Input.KeyboardAndMouse;

using WinQuickLook.Extensions;
using WinQuickLook.Messaging;
using WinQuickLook.Providers;

namespace WinQuickLook.App;

public partial class App
{
    public App(MainWindow mainWindow, LowLevelKeyboardHook keyboardHook, LowLevelMouseHook mouseHook, ShellExplorerProvider shellExplorerProvider)
    {
        InitializeComponent();

        _keyboardHook = keyboardHook;
        _mouseHook = mouseHook;
        _shellExplorerProvider = shellExplorerProvider;

        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon")!;
        ((MenuItem)_notifyIcon.ContextMenu.Items[2]).Click += (_, _) => Application.Current.Shutdown();

        _mainWindow = mainWindow;
    }

    private readonly Mutex _mutex = new(false, AppParameters.Title);

    private readonly LowLevelKeyboardHook _keyboardHook;
    private readonly LowLevelMouseHook _mouseHook;
    private readonly ShellExplorerProvider _shellExplorerProvider;

    private readonly TaskbarIcon _notifyIcon;
    private readonly MainWindow _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        if (!_mutex.WaitOne(0, false))
        {
            Shutdown();

            return;
        }

        _keyboardHook.PerformKeyDown = vkCode =>
        {
            switch (vkCode)
            {
                case VIRTUAL_KEY.VK_SPACE:
                    Dispatcher.InvokeAsync(PerformPreview);
                    break;
                case VIRTUAL_KEY.VK_ESCAPE:
                    Dispatcher.InvokeAsync(ClosePreview);
                    break;
            }
        };

        _keyboardHook.Start();

#if !DEBUG
        _mouseHook.Start();
#endif

        _mainWindow.Preload();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon.Dispose();

        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    private void PerformPreview()
    {
        var fileSystemInfo = _shellExplorerProvider.GetSelectedItem();

        if (fileSystemInfo is not null)
        {
            _mainWindow.OpenPreview(fileSystemInfo);
        }
    }

    private void ClosePreview() => _mainWindow.ClosePreviewIfActive();
}
