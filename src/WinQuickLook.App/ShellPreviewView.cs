using System;

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;

using Windows.Foundation;

using WinQuickLook.Controls;
using WinQuickLook.Handlers;

using static Microsoft.UI.Reactor.Factories;

namespace WinQuickLook.App;

public sealed class ShellPreviewView : Component<ShellPreview>
{
    public override Element Render()
    {
        var window = UseWindow() ?? throw new InvalidOperationException("A preview window is required.");
        var cleanup = UseRef<Action?>(null);
        var (failed, setFailed) = UseState(false);

        return failed
            ? Component<GenericPreviewView, System.IO.FileSystemInfo>(Props.File)
            : Border(Empty()).OnMountAdd(element =>
            {
                var border = (Microsoft.UI.Xaml.Controls.Border)element;
                ShellFileControl? session = null;
                var opened = false;

                void Resize(object? sender, object args)
                {
                    if (session is null || border.XamlRoot is null)
                    {
                        return;
                    }
                    var point = border.TransformToVisual(null).TransformPoint(new Point(0, 0));
                    var scale = border.XamlRoot.RasterizationScale;
                    session.Resize((int)Math.Round(point.X * scale), (int)Math.Round(point.Y * scale),
                        (int)Math.Round(border.ActualWidth * scale), (int)Math.Round(border.ActualHeight * scale));
                }

                void Open(object sender, RoutedEventArgs args)
                {
                    if (opened)
                    {
                        return;
                    }
                    opened = true;
                    try
                    {
                        session = new ShellFileControl(WinRT.Interop.WindowNative.GetWindowHandle(window.NativeWindow));
                        Resize(null, EventArgs.Empty);
                        if (!session.Open(Props.File))
                        {
                            session.Dispose();
                            session = null;
                            setFailed(true);
                        }
                    }
                    catch
                    {
                        session?.Dispose();
                        session = null;
                        setFailed(true);
                    }
                }

                border.Loaded += Open;
                border.LayoutUpdated += Resize;
                cleanup.Current = () =>
                {
                    border.Loaded -= Open;
                    border.LayoutUpdated -= Resize;
                    session?.Dispose();
                };
                if (border.IsLoaded)
                {
                    Open(border, new RoutedEventArgs());
                }
            }).OnUnmountAdd(_ => cleanup.Current?.Invoke()).AutomationName("Shell preview");
    }
}
