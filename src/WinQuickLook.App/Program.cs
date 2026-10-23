using System;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.UI.Reactor;

using Windows.Win32;

using WinQuickLook.Handlers;
using WinQuickLook.Messaging;
using WinQuickLook.Providers;

namespace WinQuickLook.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(false, AppParameters.Title);
        var ownsMutex = false;
        try
        {
            try { ownsMutex = mutex.WaitOne(0); }
            catch (AbandonedMutexException) { ownsMutex = true; }
            if (!ownsMutex)
            {
                return;
            }

            PInvoke.MFStartup(PInvoke.MF_VERSION, 1);
            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                using var provider = services.BuildServiceProvider();
                var app = provider.GetRequiredService<App>();
                ReactorApp.Run(_ => app.Start(args));
            }
            finally
            {
                PInvoke.MFShutdown();
            }
        }
        finally
        {
            if (ownsMutex)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.TryAddEnumerable(
        [
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, CodeFilePreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, GenericDirectoryPreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, GenericFilePreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, HtmlFilePreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, ImageFilePreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, MarkdownFilePreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, MediaFilePreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, PdfFilePreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, ShellFilePreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, SvgFilePreviewHandler>(),
            ServiceDescriptor.Singleton<IFileSystemPreviewHandler, TextFilePreviewHandler>()
        ]);

        services.AddSingleton<ShellAssociationProvider>();
        services.AddSingleton<ShellExplorerProvider>();
        services.AddSingleton<LowLevelKeyboardHook>();
        services.AddSingleton<PreviewController>();
        services.AddSingleton<App>();
    }
}
