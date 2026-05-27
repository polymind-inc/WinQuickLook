using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Graphics;
using Windows.Media.Core;
using Windows.Storage;
using Windows.System;

using WinQuickLook.Preview;

namespace WinQuickLook.App.WinUI;

public sealed partial class PreviewWindow
{
    private static readonly TimeSpan OpenAnimationDuration = TimeSpan.FromMilliseconds(180);
    private static readonly TimeSpan CrossfadeDuration = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan WindowResizeDuration = TimeSpan.FromMilliseconds(180);
    private static readonly TimeSpan WindowAnimationFrameDelay = TimeSpan.FromMilliseconds(16);
    private static readonly SizeInt32 FallbackWindowSize = new(760, 520);

    private readonly IPreviewProvider _previewProvider;
    private readonly IReadOnlyList<string> _filePaths;

    private int _currentIndex;
    private bool _isPrimaryContentActive = true;
    private bool _isSwitching;

    public PreviewWindow(IPreviewProvider previewProvider, PreviewRequest request)
    {
        InitializeComponent();

        _previewProvider = previewProvider;
        _filePaths = NormalizeFilePaths(request);
        _currentIndex = Math.Clamp(request.CurrentIndex, 0, Math.Max(_filePaths.Count - 1, 0));

        ConfigureWindow();

        Root.Loaded += Root_Loaded;
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        Root.Focus(FocusState.Programmatic);

        await ShowCurrentAsync(false);
        await PlayOpenAnimationAsync();
    }

    private void ConfigureWindow()
    {
        Title = "WinQuickLook Preview";
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DragRegion);

        AppWindow.Title = Title;
        AppWindow.IsShownInSwitchers = false;
        AppWindow.Resize(FallbackWindowSize);

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = true;
            presenter.SetBorderAndTitleBar(true, false);
        }

        AppWindow.MoveAndResize(GetCenteredBounds(FallbackWindowSize));
    }

    private async Task ShowCurrentAsync(bool crossfade)
    {
        if (_filePaths.Count == 0)
        {
            ShowUnsupported("No file was provided.");

            return;
        }

        var request = new PreviewRequest(_filePaths[_currentIndex], _filePaths, _currentIndex);

        if (!_previewProvider.TryCreatePreview(request, out var result))
        {
            ShowUnsupported("This file is not supported by the WinUI preview yet.");

            return;
        }

        try
        {
            var renderedPreview = await CreateRenderedPreviewAsync(result);

            UpdateTitle(result, result.FilePath);

            if (crossfade)
            {
                await Task.WhenAll(
                    AnimateWindowBoundsAsync(renderedPreview.WindowBounds, WindowResizeDuration),
                    CrossfadeToAsync(renderedPreview.Content));
            }
            else
            {
                PrimaryContent.Content = renderedPreview.Content;
                PrimaryContent.Opacity = 1;
                PrimaryContent.Visibility = Visibility.Visible;
                SecondaryContent.Content = null;
                SecondaryContent.Opacity = 0;
                SecondaryContent.Visibility = Visibility.Collapsed;
                _isPrimaryContentActive = true;

                await AnimateWindowBoundsAsync(renderedPreview.WindowBounds, WindowResizeDuration);
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException or IOException or ArgumentException)
        {
            ShowUnsupported("The image could not be opened.");
        }
    }

    private async Task<RenderedPreview> CreateRenderedPreviewAsync(PreviewResult result)
    {
        switch (result.Payload)
        {
            case ImagePreviewPayload imagePreviewPayload:
                var image = await LoadImageAsync(imagePreviewPayload.FilePath);

                return new RenderedPreview(CreateImageView(image), GetWindowBoundsForImage(image.PixelWidth, image.PixelHeight));
            case TextPreviewPayload textPreviewPayload:
                return new RenderedPreview(CreateTextView(textPreviewPayload), GetWindowBoundsForText(textPreviewPayload));
            case WebDocumentPreviewPayload webDocumentPreviewPayload:
                return new RenderedPreview(CreateWebDocumentView(webDocumentPreviewPayload), GetWindowBoundsForDocument());
            case MediaPreviewPayload mediaPreviewPayload:
                return new RenderedPreview(await CreateMediaViewAsync(mediaPreviewPayload), await GetWindowBoundsForMediaAsync(mediaPreviewPayload));
            case FileSystemPreviewPayload fileSystemPreviewPayload:
                return new RenderedPreview(CreateFileSystemView(fileSystemPreviewPayload), GetWindowBoundsForGenericPreview());
            default:
                return new RenderedPreview(
                    CreateUnsupportedView("This file is not supported by the WinUI preview yet."),
                    GetCenteredBounds(FallbackWindowSize));
        }
    }

    private async Task<BitmapImage> LoadImageAsync(string filePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var properties = await file.Properties.GetImagePropertiesAsync();
        var decodeSize = GetDecodeSize(properties.Width, properties.Height);

        var bitmapImage = new BitmapImage
        {
            DecodePixelWidth = decodeSize.Width,
            DecodePixelHeight = decodeSize.Height
        };

        using var stream = await file.OpenReadAsync();
        await bitmapImage.SetSourceAsync(stream);

        return bitmapImage;
    }

    private async Task CrossfadeToAsync(UIElement content)
    {
        var incoming = _isPrimaryContentActive ? SecondaryContent : PrimaryContent;
        var outgoing = _isPrimaryContentActive ? PrimaryContent : SecondaryContent;

        incoming.Content = content;
        incoming.Opacity = 0;
        incoming.Visibility = Visibility.Visible;

        await Task.WhenAll(
            AnimateOpacityAsync(outgoing, 0),
            AnimateOpacityAsync(incoming, 1));

        outgoing.Content = null;
        outgoing.Visibility = Visibility.Collapsed;
        _isPrimaryContentActive = !_isPrimaryContentActive;
    }

    private void ShowUnsupported(string message)
    {
        AppWindow.MoveAndResize(GetCenteredBounds(FallbackWindowSize));

        TitleText.Text = "Unsupported preview";
        PathText.Text = _filePaths.Count == 0 ? string.Empty : _filePaths[_currentIndex];
        CounterText.Text = string.Empty;
        PrimaryContent.Content = CreateUnsupportedView(message);
        PrimaryContent.Opacity = 1;
        PrimaryContent.Visibility = Visibility.Visible;
        SecondaryContent.Content = null;
        SecondaryContent.Opacity = 0;
        SecondaryContent.Visibility = Visibility.Collapsed;
        _isPrimaryContentActive = true;
    }

    private void UpdateTitle(PreviewResult result, string filePath)
    {
        var title = result.Title ?? Path.GetFileName(filePath);

        Title = title;
        AppWindow.Title = title;
        TitleText.Text = title;
        PathText.Text = filePath;
        CounterText.Text = _filePaths.Count > 1 ? $"{_currentIndex + 1} / {_filePaths.Count}" : string.Empty;
    }

    private async void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Escape:
            case VirtualKey.Space:
                e.Handled = true;
                Close();
                break;
            case VirtualKey.Left:
                e.Handled = true;
                await NavigateAsync(-1);
                break;
            case VirtualKey.Right:
                e.Handled = true;
                await NavigateAsync(1);
                break;
        }
    }

    private async Task NavigateAsync(int offset)
    {
        if (_isSwitching)
        {
            return;
        }

        var nextIndex = _currentIndex + offset;

        if (nextIndex < 0 || nextIndex >= _filePaths.Count)
        {
            return;
        }

        _isSwitching = true;

        try
        {
            _currentIndex = nextIndex;
            await ShowCurrentAsync(true);
        }
        finally
        {
            _isSwitching = false;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task PlayOpenAnimationAsync()
    {
        await Task.WhenAll(
            AnimateOpacityAsync(PreviewSurface, 1, OpenAnimationDuration),
            AnimateScaleAsync(1, OpenAnimationDuration));
    }

    private static Task AnimateOpacityAsync(UIElement target, double to)
    {
        return AnimateOpacityAsync(target, to, CrossfadeDuration);
    }

    private static Task AnimateOpacityAsync(UIElement target, double to, TimeSpan duration)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));
        storyboard.Children.Add(animation);

        return BeginStoryboardAsync(storyboard);
    }

    private Task AnimateScaleAsync(double to, TimeSpan duration)
    {
        var storyboard = new Storyboard();

        foreach (var propertyName in new[] { nameof(ScaleTransform.ScaleX), nameof(ScaleTransform.ScaleY) })
        {
            var animation = new DoubleAnimation
            {
                To = to,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(animation, PreviewSurfaceScale);
            Storyboard.SetTargetProperty(animation, propertyName);
            storyboard.Children.Add(animation);
        }

        return BeginStoryboardAsync(storyboard);
    }

    private static Task BeginStoryboardAsync(Storyboard storyboard)
    {
        var completion = new TaskCompletionSource();

        storyboard.Completed += (_, _) => completion.TrySetResult();
        storyboard.Begin();

        return completion.Task;
    }

    private static UIElement CreateImageView(BitmapImage image)
    {
        return new Image
        {
            Source = image,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Stretch = Stretch.Uniform
        };
    }

    private static UIElement CreateTextView(TextPreviewPayload payload)
    {
        var text = payload.IsTruncated
            ? payload.Text + Environment.NewLine + Environment.NewLine + "[Preview truncated at 1 MB]"
            : payload.Text;
        var fontFamily = new FontFamily("Cascadia Mono, Consolas");
        var lineCount = CountLines(text);
        var grid = new Grid
        {
            ColumnSpacing = 14
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lineNumbers = new TextBlock
        {
            Text = string.Join(Environment.NewLine, Enumerable.Range(1, lineCount)),
            FontFamily = fontFamily,
            FontSize = 13,
            IsHitTestVisible = false,
            Opacity = 0.42,
            TextAlignment = TextAlignment.Right
        };
        var textBlock = new TextBlock
        {
            Text = text,
            FontFamily = fontFamily,
            FontSize = 13,
            IsTextSelectionEnabled = true,
            TextWrapping = TextWrapping.NoWrap
        };

        Grid.SetColumn(textBlock, 1);
        grid.Children.Add(lineNumbers);
        grid.Children.Add(textBlock);

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(10),
            Content = grid
        };
    }

    private static UIElement CreateUnsupportedView(string message)
    {
        var stackPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8
        };

        stackPanel.Children.Add(new FontIcon
        {
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 32,
            Glyph = "\uE8A5",
            Opacity = 0.7
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 14,
            Opacity = 0.82,
            TextAlignment = TextAlignment.Center
        });

        return stackPanel;
    }

    private static UIElement CreateWebDocumentView(WebDocumentPreviewPayload payload)
    {
        var webView = new WebView2
        {
            DefaultBackgroundColor = Microsoft.UI.Colors.Transparent
        };

        if (payload.Html is not null)
        {
            webView.Loaded += async (_, _) =>
            {
                await webView.EnsureCoreWebView2Async();
                webView.CoreWebView2.NavigateToString(payload.Html);
            };
        }
        else
        {
            webView.Source = new Uri(payload.FilePath);
        }

        return webView;
    }

    private static async Task<UIElement> CreateMediaViewAsync(MediaPreviewPayload payload)
    {
        var mediaPlayerElement = new MediaPlayerElement
        {
            AreTransportControlsEnabled = true,
            AutoPlay = true,
            Source = MediaSource.CreateFromUri(new Uri(payload.FilePath)),
            Stretch = Stretch.Uniform
        };

        if (payload.HasVideo)
        {
            return mediaPlayerElement;
        }

        var file = await StorageFile.GetFileFromPathAsync(payload.FilePath);
        var properties = await file.Properties.GetMusicPropertiesAsync();
        var title = string.IsNullOrWhiteSpace(properties.Title) ? Path.GetFileName(payload.FilePath) : properties.Title;
        var artist = string.IsNullOrWhiteSpace(properties.Artist) ? "Unknown artist" : properties.Artist;
        var album = string.IsNullOrWhiteSpace(properties.Album) ? string.Empty : properties.Album;
        var grid = new Grid
        {
            Padding = new Thickness(28)
        };

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var stackPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10
        };

        stackPanel.Children.Add(new FontIcon
        {
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 54,
            Glyph = "\uE8D6",
            Opacity = 0.72
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(album) ? artist : $"{artist} · {album}",
            FontSize = 14,
            Opacity = 0.72,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        Grid.SetRow(mediaPlayerElement, 1);
        grid.Children.Add(stackPanel);
        grid.Children.Add(mediaPlayerElement);

        return grid;
    }

    private static UIElement CreateFileSystemView(FileSystemPreviewPayload payload)
    {
        var grid = new Grid
        {
            Padding = new Thickness(28),
            ColumnSpacing = 24
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.42, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.58, GridUnitType.Star) });

        var icon = new FontIcon
        {
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 96,
            Glyph = payload.IsDirectory ? "\uE8B7" : "\uE7C3",
            Opacity = 0.72,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var stackPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 12
        };

        stackPanel.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(payload.Path),
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        stackPanel.Children.Add(CreateMetadataText(payload.TypeName));
        stackPanel.Children.Add(CreateMetadataText(payload.Detail));
        stackPanel.Children.Add(CreateMetadataText(payload.LastWriteTime.ToString("G")));

        Grid.SetColumn(stackPanel, 1);
        grid.Children.Add(icon);
        grid.Children.Add(stackPanel);

        return grid;
    }

    private static TextBlock CreateMetadataText(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 15,
            Opacity = 0.76,
            TextWrapping = TextWrapping.Wrap
        };
    }

    private RectInt32 GetWindowBoundsForImage(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return GetCenteredBounds(FallbackWindowSize);
        }

        var scale = Root.XamlRoot?.RasterizationScale ?? 1.0;
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var maxWidth = Math.Min(displayArea.WorkArea.Width / scale * 0.82, 1220);
        var maxHeight = Math.Min(displayArea.WorkArea.Height / scale * 0.82, 840);
        var maxImageWidth = Math.Max(maxWidth - 68, 360);
        var maxImageHeight = Math.Max(maxHeight - 118, 260);
        var imageScale = Math.Min(Math.Min(maxImageWidth / pixelWidth, maxImageHeight / pixelHeight), 1.0);
        var contentWidth = Math.Round(pixelWidth * imageScale);
        var contentHeight = Math.Round(pixelHeight * imageScale);
        var windowWidth = Math.Clamp(contentWidth + 68, 500, maxWidth);
        var windowHeight = Math.Clamp(contentHeight + 118, 360, maxHeight);
        var windowSize = new SizeInt32(
            (int)Math.Round(windowWidth * scale),
            (int)Math.Round(windowHeight * scale));

        return GetCenteredBounds(windowSize);
    }

    private RectInt32 GetWindowBoundsForDocument()
    {
        return GetCenteredBounds(GetConstrainedWindowSize(980, 720));
    }

    private RectInt32 GetWindowBoundsForGenericPreview()
    {
        return GetCenteredBounds(GetConstrainedWindowSize(640, 380));
    }

    private async Task<RectInt32> GetWindowBoundsForMediaAsync(MediaPreviewPayload payload)
    {
        if (!payload.HasVideo)
        {
            return GetCenteredBounds(GetConstrainedWindowSize(700, 420));
        }

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(payload.FilePath);
            var properties = await file.Properties.GetVideoPropertiesAsync();

            if (properties.Width > 0 && properties.Height > 0)
            {
                return GetWindowBoundsForImage((int)properties.Width, (int)properties.Height);
            }
        }
        catch (FileNotFoundException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return GetCenteredBounds(GetConstrainedWindowSize(960, 620));
    }

    private SizeInt32 GetConstrainedWindowSize(double requestedWidth, double requestedHeight)
    {
        var scale = Root.XamlRoot?.RasterizationScale ?? 1.0;
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var maxWidth = displayArea.WorkArea.Width / scale * 0.82;
        var maxHeight = displayArea.WorkArea.Height / scale * 0.82;

        return new SizeInt32(
            (int)Math.Round(Math.Min(requestedWidth, maxWidth) * scale),
            (int)Math.Round(Math.Min(requestedHeight, maxHeight) * scale));
    }

    private RectInt32 GetWindowBoundsForText(TextPreviewPayload payload)
    {
        var scale = Root.XamlRoot?.RasterizationScale ?? 1.0;
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var maxWidth = Math.Min(displayArea.WorkArea.Width / scale * 0.82, 1180);
        var maxHeight = Math.Min(displayArea.WorkArea.Height / scale * 0.82, 820);
        var lineCount = CountLines(payload.Text);
        var longestLineLength = GetLongestLineLength(payload.Text);
        var windowWidth = Math.Clamp((longestLineLength * 7.4) + 190, 720, maxWidth);
        var windowHeight = Math.Clamp((Math.Min(lineCount, 34) * 19.0) + 132, 500, maxHeight);
        var windowSize = new SizeInt32(
            (int)Math.Round(windowWidth * scale),
            (int)Math.Round(windowHeight * scale));

        return GetCenteredBounds(windowSize);
    }

    private RectInt32 GetCenteredBounds(SizeInt32 windowSize)
    {
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var x = workArea.X + ((workArea.Width - windowSize.Width) / 2);
        var y = workArea.Y + ((workArea.Height - windowSize.Height) / 2);

        return new RectInt32(x, y, windowSize.Width, windowSize.Height);
    }

    private async Task AnimateWindowBoundsAsync(RectInt32 targetBounds, TimeSpan duration)
    {
        var startPosition = AppWindow.Position;
        var startSize = AppWindow.Size;
        var startBounds = new RectInt32(startPosition.X, startPosition.Y, startSize.Width, startSize.Height);

        if (duration <= TimeSpan.Zero || AreBoundsEqual(startBounds, targetBounds))
        {
            AppWindow.MoveAndResize(targetBounds);

            return;
        }

        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var progress = Math.Min(stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
            var easedProgress = EaseOutCubic(progress);

            AppWindow.MoveAndResize(InterpolateBounds(startBounds, targetBounds, easedProgress));

            if (progress >= 1.0)
            {
                break;
            }

            await Task.Delay(WindowAnimationFrameDelay);
        }

        AppWindow.MoveAndResize(targetBounds);
    }

    private static RectInt32 InterpolateBounds(RectInt32 from, RectInt32 to, double progress)
    {
        return new RectInt32(
            Interpolate(from.X, to.X, progress),
            Interpolate(from.Y, to.Y, progress),
            Interpolate(from.Width, to.Width, progress),
            Interpolate(from.Height, to.Height, progress));
    }

    private static int Interpolate(int from, int to, double progress)
    {
        return (int)Math.Round(from + ((to - from) * progress));
    }

    private static double EaseOutCubic(double progress)
    {
        return 1.0 - Math.Pow(1.0 - progress, 3);
    }

    private static bool AreBoundsEqual(RectInt32 left, RectInt32 right)
    {
        return left.X == right.X
            && left.Y == right.Y
            && left.Width == right.Width
            && left.Height == right.Height;
    }

    private static int CountLines(string text)
    {
        return string.IsNullOrEmpty(text) ? 1 : text.Count(x => x == '\n') + 1;
    }

    private static int GetLongestLineLength(string text)
    {
        var longestLineLength = 0;
        var currentLineLength = 0;

        foreach (var character in text)
        {
            if (character == '\r')
            {
                continue;
            }

            if (character == '\n')
            {
                longestLineLength = Math.Max(longestLineLength, currentLineLength);
                currentLineLength = 0;

                continue;
            }

            currentLineLength++;
        }

        return Math.Max(longestLineLength, currentLineLength);
    }

    private static SizeInt32 GetDecodeSize(uint pixelWidth, uint pixelHeight)
    {
        const double maxDecodedPixels = 1800;

        if (pixelWidth == 0 || pixelHeight == 0)
        {
            return new SizeInt32(0, 0);
        }

        var scale = Math.Min(Math.Min(maxDecodedPixels / pixelWidth, maxDecodedPixels / pixelHeight), 1.0);

        return new SizeInt32(
            Math.Max(1, (int)Math.Round(pixelWidth * scale)),
            Math.Max(1, (int)Math.Round(pixelHeight * scale)));
    }

    private static IReadOnlyList<string> NormalizeFilePaths(PreviewRequest request)
    {
        if (request.SiblingFilePaths is { Count: > 0 })
        {
            return request.SiblingFilePaths;
        }

        return string.IsNullOrWhiteSpace(request.FilePath)
            ? []
            : [request.FilePath];
    }

    private sealed record RenderedPreview(UIElement Content, RectInt32 WindowBounds);
}
