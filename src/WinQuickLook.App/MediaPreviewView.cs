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
        var (width, height) = UseWindowSize();
        var dipScale = UseDpi() / 96.0;
        var compact = width < 520 || height < 560;
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

        Element media = MediaPlayerElement().Set(control =>
        {
            element.Current = control;
            control.SetMediaPlayer(player);
            control.AreTransportControlsEnabled = true;
            control.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform;
        }).MediaFailed(setError).AutomationName(Props.IsVideo ? "Video preview" : "Audio preview");
        if (Props.IsVideo && Props.PixelSize is { } size)
        {
            media = media.MaxWidth(size.Width / dipScale).MaxHeight(size.Height / dipScale);
        }

        Element artwork = thumbnail is null
            ? Border(TextBlock("\uE8D6").FontFamily("Segoe Fluent Icons").FontSize(56).Foreground(Theme.SecondaryText)
                .HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center))
                .Background(Theme.SubtleFill).CornerRadius(8).WithBorder(Theme.CardStroke)
            : Component<BitmapPreview, BitmapPreviewProps>(new(thumbnail));
        artwork = Border(artwork).CornerRadius(12).Size(compact ? 160 : 224, compact ? 160 : 224);

        var title = VStack(8,
            Body(metadata?.Title ?? Props.File.Name).FontSize(18).SemiBold()
                .TextWrapping(TextWrapping.Wrap).TextAlignment(TextAlignment.Center),
            Body(metadata?.Artist ?? "Unknown artist").Foreground(Theme.SecondaryText)
                .TextWrapping(TextWrapping.Wrap).TextAlignment(TextAlignment.Center),
            string.IsNullOrEmpty(metadata?.Album) ? null :
                Caption(metadata.Album).Foreground(Theme.SecondaryText)
                    .TextWrapping(TextWrapping.Wrap).TextAlignment(TextAlignment.Center)
        );
        var audio = VStack(20, artwork.HAlign(HorizontalAlignment.Center), title)
            .Width(Math.Max(0, Math.Min(width - 48, 480))).HAlign(HorizontalAlignment.Center);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star(), GridSize.Auto],
            Props.IsVideo ? null : ScrollViewer(audio.Margin(24))
                .Set(viewer =>
                {
                    viewer.VerticalContentAlignment = VerticalAlignment.Center;
                    viewer.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                    viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                }).Grid(row: 0),
            Props.IsVideo ? media.Grid(row: 0, rowSpan: 2) : media.Height(64).Margin(16, 0, 16, 8).Grid(row: 1),
            error is null ? null : InfoBar("Cannot play media", error).Severity(InfoBarSeverity.Error)
                .Margin(16).VAlign(VerticalAlignment.Top).Grid(row: 0)
        );
    }
}
