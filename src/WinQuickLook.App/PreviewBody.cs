using System;
using System.Globalization;
using System.IO;
using System.Linq;

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Foundation;

using WinQuickLook.Handlers;
using WinQuickLook.Providers;

using static Microsoft.UI.Reactor.Factories;

namespace WinQuickLook.App;

public sealed class PreviewBody : Component<PreviewContent>
{
    public override Element Render() => Props switch
    {
        TextPreview text => Component<BrowserPreview, BrowserPreviewProps>(
            new(PreviewHtml.Source(text.Text, text.Language), null, false)),
        HtmlPreview html => Component<DocumentPreview, HtmlPreview>(html),
        ImagePreview image => Component<BitmapPreview, BitmapPreviewProps>(new(image.Data, image.PixelSize))
            .Margin(AppParameters.ImagePadding),
        MediaPreview media => Component<MediaPreviewView, MediaPreview>(media),
        ShellPreview shell => Component<ShellPreviewView, ShellPreview>(shell),
        GenericPreview generic => Component<GenericPreviewView, FileSystemInfo>(generic.FileSystemInfo),
        _ => Empty()
    };
}

public sealed class DocumentPreview : Component<HtmlPreview>
{
    public override Element Render()
    {
        var (selected, setSelected) = UseState(0);
        var preview = Props.Html is null
            ? new BrowserPreviewProps(null, new Uri(Props.File.FullName), true)
            : new BrowserPreviewProps(PreviewHtml.Document(Props.Html), null, false, Props.File.DirectoryName);
        var source = new BrowserPreviewProps(
            PreviewHtml.Source(Props.Source, Props.Html is null ? "html" : null), null, false);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Auto, GridSize.Star()],
            SelectorBar([SelectorBarItem("Preview"), SelectorBarItem("Source")], selected, setSelected)
                .AutomationName("Document view").HAlign(HorizontalAlignment.Center).Margin(8, 4).Grid(row: 0),
            Component<BrowserPreview, BrowserPreviewProps>(selected == 0 ? preview : source)
                .WithKey(selected == 0 ? "preview" : "source").Grid(row: 1)
        );
    }
}

public sealed record BitmapPreviewProps(byte[] Data, Size? PixelSize = null);

public sealed class BitmapPreview : Component<BitmapPreviewProps>
{
    public override Element Render()
    {
        var dipScale = UseDpi() / 96.0;
        var bitmap = UseMemo(() =>
        {
            using var stream = new MemoryStream(Props.Data);
            var image = new BitmapImage();
            image.SetSource(stream.AsRandomAccessStream());
            return image;
        }, Props.Data);

        Element image = BitmapImageElement.Image().Set(image =>
        {
            image.Source = bitmap;
            image.Stretch = Stretch.Uniform;
        }).AutomationName("Image preview");
        return Props.PixelSize is { } size
            ? image.MaxWidth(size.Width / dipScale).MaxHeight(size.Height / dipScale)
            : image;
    }
}

public sealed class GenericPreviewView : Component<FileSystemInfo>
{
    public override Element Render()
    {
        var (width, height) = UseWindowSize();
        var compact = width < 640 || height < 560;
        var thumbnail = UseMemo(() => new ShellThumbnailProvider().GetImage(Props), Props);
        var typeName = UseMemo(() =>
        {
            if (Props is DirectoryInfo)
            {
                return "Folder";
            }
            var type = new Windows.Win32.UI.Shell.SHFILEINFOW();
            Windows.Win32.PInvoke.SHGetFileInfo(Props.FullName, 0, ref type,
                Windows.Win32.UI.Shell.SHGFI_FLAGS.SHGFI_TYPENAME | Windows.Win32.UI.Shell.SHGFI_FLAGS.SHGFI_USEFILEATTRIBUTES);
            return type.szTypeName.ToString();
        }, Props);

        Element artwork = thumbnail is null
            ? TextBlock(Props is DirectoryInfo ? "\uE8B7" : "\uE8A5")
                .FontFamily("Segoe Fluent Icons").FontSize(56).Foreground(Theme.SecondaryText)
                .HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center)
            : Component<BitmapPreview, BitmapPreviewProps>(new(thumbnail));
        artwork = Border(artwork).Size(compact ? 112 : 192, compact ? 112 : 192);

        var heading = VStack(8,
            Subtitle(Props.Name).TextWrapping(TextWrapping.Wrap)
                .TextAlignment(TextAlignment.Center),
            Body(typeName).Foreground(Theme.SecondaryText)
                .TextAlignment(TextAlignment.Center)
        );
        var summary = VStack(20, artwork.HAlign(HorizontalAlignment.Center), heading);

        var properties = VStack(8,
            Property(Props is DirectoryInfo ? "Contents" : "Size", Component<FileDetails, FileSystemInfo>(Props)),
            Property("Modified", Caption(Props.LastWriteTime.ToString("g", CultureInfo.CurrentUICulture)))
        ).MaxWidth(320).HAlign(HorizontalAlignment.Center);

        return ScrollViewer(
            VStack(24, summary, properties).Width(Math.Max(0, Math.Min(width - (compact ? 48 : 64), 480)))
                .HAlign(HorizontalAlignment.Center).Margin(compact ? 24 : 32)
        ).Set(viewer =>
        {
            viewer.VerticalContentAlignment = VerticalAlignment.Center;
            viewer.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            viewer.HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Disabled;
        });
    }

    private static Element Property(string label, Element value) =>
        Grid(columns: [GridSize.Px(80), GridSize.Star()], rows: [GridSize.Auto],
            Caption(label).Foreground(Theme.SecondaryText).Grid(column: 0),
            value.Grid(column: 1));
}

public sealed class FileDetails : Component<FileSystemInfo>
{
    public override Element Render()
    {
        var details = UseResource(
            cancellation => System.Threading.Tasks.Task.Run(() =>
            {
                cancellation.ThrowIfCancellationRequested();
                return Props is DirectoryInfo directory
                    ? directory.EnumerateFileSystemInfos().Count().ToString("N0") + " items"
                    : FormatSize(((FileInfo)Props).Length);
            }, cancellation),
            [Props.FullName]);

        return details.Match<Element>(
            loading: () => Caption("Loading…").Foreground(Theme.SecondaryText),
            loaded: value => Caption(value).Foreground(Theme.SecondaryText),
            error: _ => Caption("Details unavailable").Foreground(Theme.SecondaryText));
    }

    private static string FormatSize(long size) => size switch
    {
        >= 1L << 40 => $"{size / (double)(1L << 40):0.##} TB",
        >= 1L << 30 => $"{size / (double)(1L << 30):0.##} GB",
        >= 1L << 20 => $"{size / (double)(1L << 20):0.##} MB",
        >= 1L << 10 => $"{size / (double)(1L << 10):0.##} KB",
        _ => $"{size} B"
    };
}
