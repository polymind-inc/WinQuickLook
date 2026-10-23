using System;
using System.Threading.Tasks;

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using static Microsoft.UI.Reactor.Factories;

namespace WinQuickLook.App;

public sealed record BrowserPreviewProps(string? Html, Uri? Source, bool AllowScripts);

public sealed class BrowserPreview : Component<BrowserPreviewProps>
{
    public override Element Render()
    {
        var loaded = UseRef<RoutedEventHandler?>(null);
        var started = UseRef(false);
        var disposed = UseRef(false);
        var (error, setError) = UseState<string?>(null);

        async Task InitializeAsync(Microsoft.UI.Xaml.Controls.WebView2 webView)
        {
            if (started.Current)
            {
                return;
            }
            started.Current = true;
            try
            {
                await webView.EnsureCoreWebView2Async();
                if (disposed.Current)
                {
                    return;
                }
                webView.CoreWebView2.Settings.IsScriptEnabled = Props.AllowScripts;
                if (Props.Source is { } source)
                {
                    webView.CoreWebView2.Navigate(source.AbsoluteUri);
                }
                else if (Props.Html is { } html)
                {
                    webView.NavigateToString(html);
                }
            }
            catch (Exception exception)
            {
                if (!disposed.Current)
                {
                    setError(exception.Message);
                }
            }
        }

        return error is null
            ? WebView2().OnMountAdd(element =>
            {
                var control = (Microsoft.UI.Xaml.Controls.WebView2)element;
                loaded.Current = async (_, _) => await InitializeAsync(control);
                control.Loaded += loaded.Current;
                if (control.IsLoaded)
                {
                    _ = InitializeAsync(control);
                }
            }).OnUnmountAdd(element =>
            {
                disposed.Current = true;
                var control = (Microsoft.UI.Xaml.Controls.WebView2)element;
                control.Loaded -= loaded.Current;
                control.Close();
            }).AutomationName("Document preview")
            : InfoBar("Preview unavailable", error).Severity(InfoBarSeverity.Error);
    }
}
