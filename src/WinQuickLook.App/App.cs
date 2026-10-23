using System;
using System.Diagnostics;
using System.IO;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Reactor;

using Windows.Win32.UI.Input.KeyboardAndMouse;

using WinQuickLook.Messaging;
using WinQuickLook.Providers;

using static Microsoft.UI.Reactor.Factories;

namespace WinQuickLook.App;

public sealed class App(LowLevelKeyboardHook keyboardHook, ShellExplorerProvider shellExplorerProvider, PreviewController previewController) : IDisposable
{
    private ReactorTrayIcon? _trayIcon;

    public void Start(string[] args)
    {
        var dispatcher = DispatcherQueue.GetForCurrentThread();
        ReactorApp.ShutdownPolicy = ShutdownPolicy.Explicit;
        _trayIcon = ReactorApp.OpenTrayIcon(new TrayIconSpec(
            WindowIcon.FromPath(Path.Combine(AppContext.BaseDirectory, "Icon.ico")), AppParameters.Title));

        _trayIcon.RightClick += (_, _) => _trayIcon.ShowFlyout(MenuItems(
            MenuItem("Exit", () => ReactorApp.Exit())));
        keyboardHook.PerformKeyDown = key =>
        {
            if (key == VIRTUAL_KEY.VK_SPACE)
            {
                dispatcher.TryEnqueue(PerformPreview);
            }
            else if (key == VIRTUAL_KEY.VK_ESCAPE)
            {
                dispatcher.TryEnqueue(previewController.ClosePreviewIfActive);
            }
        };
        keyboardHook.Start();

        if (args.Length > 0)
        {
            var path = Path.GetFullPath(args[0]);
            if (File.Exists(path))
            {
                previewController.OpenPreview(new FileInfo(path));
            }
            else if (Directory.Exists(path))
            {
                previewController.OpenPreview(new DirectoryInfo(path));
            }
        }
    }

    private void PerformPreview()
    {
        try
        {
            if (shellExplorerProvider.GetSelectedItem() is { } item)
            {
                previewController.OpenPreview(item);
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }

    public void Dispose()
    {
        keyboardHook.PerformKeyDown = null;
        keyboardHook.Stop();
        _trayIcon?.Dispose();
    }
}
