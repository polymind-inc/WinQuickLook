using System;

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Media.Core;
using Windows.Media.Playback;

using WinQuickLook.Handlers;
using WinQuickLook.Providers;

using static Microsoft.UI.Reactor.Factories;

namespace WinQuickLook.App;

public sealed class MediaPreviewView : Component<MediaPreview>
{
    public override Element Render()
    {
        var (width, _) = UseWindowSize();
        var compact = width < 640;
        var element = UseRef<Microsoft.UI.Xaml.Controls.MediaPlayerElement?>(null);
        var source = UseMemo(() => MediaSource.CreateFromUri(new Uri(Props.File.FullName)), Props);
        var player = UseMemo(() => new MediaPlayer
        {
            Source = source,
            AutoPlay = true
        }, Props);
        UseEffect(() => () =>
        {
            element.Current?.SetMediaPlayer(null);
            player.Source = null;
            player.Dispose();
            source.Dispose();
        }, player);

        var metadata = UseMemo(() => Props.IsVideo ? null : new ShellPropertyProvider().GetAudioProperties(Props.File), Props);
        var thumbnail = UseMemo(() => Props.IsVideo ? null : new ShellThumbnailProvider().GetImage(Props.File), Props);
        var (error, setError) = UseState<string?>(null);

        var media = MediaPlayerElement().Set(control =>
        {
            element.Current = control;
            control.SetMediaPlayer(player);
            control.AreTransportControlsEnabled = true;
        }).MediaFailed(setError).AutomationName(Props.IsVideo ? "Video preview" : "Audio preview");

        Element artwork = thumbnail is null
            ? Border(TextBlock("\uE8D6").FontFamily("Segoe Fluent Icons").FontSize(56).Foreground(Theme.SecondaryText)
                .HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center))
                .Background(Theme.SubtleFill).CornerRadius(8).WithBorder(Theme.CardStroke)
            : Component<BitmapPreview, byte[]>(thumbnail);
        artwork = artwork.Size(compact ? 128 : 192, compact ? 128 : 192);

        var title = VStack(8,
            Subtitle(metadata?.Title ?? Props.File.Name).TextWrapping(TextWrapping.Wrap)
                .TextAlignment(compact ? TextAlignment.Center : TextAlignment.Left),
            Body(metadata?.Artist ?? "Unknown artist").Foreground(Theme.SecondaryText)
                .TextWrapping(TextWrapping.Wrap).TextAlignment(compact ? TextAlignment.Center : TextAlignment.Left),
            string.IsNullOrEmpty(metadata?.Album) ? null :
                Caption(metadata.Album).Foreground(Theme.SecondaryText)
                    .TextWrapping(TextWrapping.Wrap).TextAlignment(compact ? TextAlignment.Center : TextAlignment.Left)
        );
        Element audio = compact
            ? VStack(16, artwork.HAlign(HorizontalAlignment.Center), title)
            : Grid(columns: [GridSize.Px(192), GridSize.Star()], rows: [GridSize.Auto],
                artwork.Grid(column: 0),
                title.Margin(left: 24).VAlign(VerticalAlignment.Center).Grid(column: 1));

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star(), GridSize.Auto],
            Props.IsVideo ? null : ScrollViewer(audio.Margin(24))
                .Set(viewer => viewer.VerticalContentAlignment = VerticalAlignment.Center).Grid(row: 0),
            Props.IsVideo ? media.Grid(row: 0, rowSpan: 2) : media.Height(80).Grid(row: 1),
            error is null ? null : InfoBar("Cannot play media", error).Severity(InfoBarSeverity.Error)
                .Margin(16).VAlign(VerticalAlignment.Top).Grid(row: 0)
        );
    }
}
