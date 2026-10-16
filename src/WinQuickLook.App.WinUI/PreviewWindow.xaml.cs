using System;
using System.Collections.Generic;
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

using WinRT.Interop;

namespace WinQuickLook.App.WinUI;

public sealed partial class PreviewWindow
{
    private static readonly TimeSpan CrossfadeDuration = TimeSpan.FromMilliseconds(140);
    private static readonly SizeInt32 FallbackWindowSize = new(760, 520);

    private readonly IPreviewProvider _previewProvider;
    private readonly IReadOnlyList<string> _filePaths;
    private readonly nint _hwnd;

    private int _currentIndex;
    private bool _isPrimaryContentActive = true;
    private bool _isSwitching;
    private bool _revealed;

    public PreviewWindow(IPreviewProvider previewProvider, PreviewRequest request)
    {
        InitializeComponent();

        _previewProvider = previewProvider;
        _filePaths = NormalizeFilePaths(request);
        _currentIndex = Math.Clamp(request.CurrentIndex, 0, Math.Max(_filePaths.Count - 1, 0));

        _hwnd = WindowNative.GetWindowHandle(this);
        WindowComposition.SetCloaked(_hwnd, true);
        WindowComposition.EnableRoundedCorners(_hwnd);

        ConfigureWindow();
        ConfigureNavigationChrome();

        Root.Loaded += Root_Loaded;
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        Root.Focus(FocusState.Programmatic);

        await ShowCurrentAsync(false);
        await RevealWindowAsync();
    }

    private async Task RevealWindowAsync()
    {
        if (_revealed)
        {
            return;
        }

        _revealed = true;

        // Yield once so the layout pass that follows MoveAndResize has been
        // composited by DWM before we uncloak — otherwise the first visible
        // frame can show the previous (fallback) size with Mica not yet
        // recomputed for the new bounds.
        await Task.Yield();

        WindowComposition.SetCloaked(_hwnd, false);
    }

    private void ConfigureWindow()
    {
        Title = "WinQuickLook Preview";
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DragRegion);

        AppWindow.Title = Title;
        AppWindow.IsShownInSwitchers = false;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = true;
            presenter.SetBorderAndTitleBar(true, false);
        }

        AppWindow.MoveAndResize(GetCenteredBounds(FallbackWindowSize));
    }

    private void ConfigureNavigationChrome()
    {
        var hasSiblings = _filePaths.Count > 1;
        var visibility = hasSiblings ? Visibility.Visible : Visibility.Collapsed;

        CounterPill.Visibility = visibility;
        PrevButton.Visibility = visibility;
        NextButton.Visibility = visibility;
        HeaderSeparator.Visibility = visibility;

        UpdateNavigationState();
    }

    private void UpdateNavigationState()
    {
        if (_filePaths.Count <= 1)
        {
            return;
        }

        PrevButton.IsEnabled = _currentIndex > 0;
        NextButton.IsEnabled = _currentIndex < _filePaths.Count - 1;
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

            UpdateHeader(result, result.FilePath);

            var sizeChanging = IsBoundsChanging(renderedPreview.WindowBounds);

            if (crossfade && !sizeChanging)
            {
                // Same size — content crossfade looks clean.
                await CrossfadeToAsync(renderedPreview.Content);
            }
            else if (crossfade)
            {
                // Resize is visible; cloak the window across the resize +
                // content swap so the user never sees a half-laid-out frame
                // or a Mica recompute flash.
                WindowComposition.SetCloaked(_hwnd, true);
                try
                {
                    SwapContent(renderedPreview.Content);
                    AppWindow.MoveAndResize(renderedPreview.WindowBounds);
                    await Task.Yield();
                }
                finally
                {
                    WindowComposition.SetCloaked(_hwnd, false);
                }
            }
            else
            {
                SwapContent(renderedPreview.Content);
                AppWindow.MoveAndResize(renderedPreview.WindowBounds);
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException or IOException or ArgumentException)
        {
            ShowUnsupported("The file could not be opened.");
        }
    }

    private bool IsBoundsChanging(RectInt32 target)
    {
        var position = AppWindow.Position;
        var size = AppWindow.Size;

        return position.X != target.X
            || position.Y != target.Y
            || size.Width != target.Width
            || size.Height != target.Height;
    }

    private void SwapContent(UIElement content)
    {
        PrimaryContent.Content = content;
        PrimaryContent.Opacity = 1;
        PrimaryContent.Visibility = Visibility.Visible;
        SecondaryContent.Content = null;
        SecondaryContent.Opacity = 0;
        SecondaryContent.Visibility = Visibility.Collapsed;
        _isPrimaryContentActive = true;
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

        var fallbackPath = _filePaths.Count == 0 ? string.Empty : _filePaths[_currentIndex];

        TitleText.Text = string.IsNullOrEmpty(fallbackPath) ? "Unsupported preview" : Path.GetFileName(fallbackPath);
        SubtitleText.Text = string.IsNullOrEmpty(fallbackPath)
            ? string.Empty
            : (Path.GetDirectoryName(fallbackPath) ?? string.Empty);
        PrimaryContent.Content = CreateUnsupportedView(message);
        PrimaryContent.Opacity = 1;
        PrimaryContent.Visibility = Visibility.Visible;
        SecondaryContent.Content = null;
        SecondaryContent.Opacity = 0;
        SecondaryContent.Visibility = Visibility.Collapsed;
        _isPrimaryContentActive = true;
    }

    private void UpdateHeader(PreviewResult result, string filePath)
    {
        var title = result.Title ?? Path.GetFileName(filePath);

        Title = title;
        AppWindow.Title = title;
        TitleText.Text = title;
        SubtitleText.Text = BuildSubtitle(filePath);

        if (_filePaths.Count > 1)
        {
            CounterText.Text = $"{_currentIndex + 1} / {_filePaths.Count}";
        }

        UpdateNavigationState();
    }

    private static string BuildSubtitle(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);

        return string.IsNullOrEmpty(directory) ? filePath : directory;
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

    private async void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        await NavigateAsync(-1);
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        await NavigateAsync(1);
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

    private static Task AnimateOpacityAsync(UIElement target, double to)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = CrossfadeDuration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));
        storyboard.Children.Add(animation);

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
            Stretch = Stretch.Uniform,
            Margin = new Thickness(8)
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
            FontSize = 12.5,
            IsHitTestVisible = false,
            Opacity = 0.4,
            TextAlignment = TextAlignment.Right
        };
        var textBlock = new TextBlock
        {
            Text = text,
            FontFamily = fontFamily,
            FontSize = 12.5,
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
            Padding = new Thickness(16, 14, 16, 14),
            Content = grid
        };
    }

    private static UIElement CreateUnsupportedView(string message)
    {
        var stackPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10
        };

        stackPanel.Children.Add(new FontIcon
        {
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 36,
            Glyph = "\uE7BA",
            Opacity = 0.55
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 13,
            Opacity = 0.78,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 360
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
            mediaPlayerElement.Margin = new Thickness(0);

            return mediaPlayerElement;
        }

        var file = await StorageFile.GetFileFromPathAsync(payload.FilePath);
        var properties = await file.Properties.GetMusicPropertiesAsync();
        var title = string.IsNullOrWhiteSpace(properties.Title) ? Path.GetFileNameWithoutExtension(payload.FilePath) : properties.Title;
        var artist = string.IsNullOrWhiteSpace(properties.Artist) ? "Unknown artist" : properties.Artist;
        var album = string.IsNullOrWhiteSpace(properties.Album) ? string.Empty : properties.Album;
        var grid = new Grid
        {
            Padding = new Thickness(32, 28, 32, 24),
            RowSpacing = 18
        };

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var stackPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8
        };

        var artworkContainer = new Border
        {
            Width = 132,
            Height = 132,
            CornerRadius = new CornerRadius(16),
            Background = Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Brush,
            Margin = new Thickness(0, 0, 0, 8)
        };

        artworkContainer.Child = new FontIcon
        {
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 56,
            Glyph = "\uE8D6",
            Opacity = 0.72
        };

        stackPanel.Children.Add(artworkContainer);
        stackPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 360
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(album) ? artist : $"{artist} · {album}",
            FontSize = 13,
            Opacity = 0.7,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 360
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
            Padding = new Thickness(32, 28, 32, 28),
            ColumnSpacing = 28
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconContainer = new Border
        {
            Width = 132,
            Height = 132,
            CornerRadius = new CornerRadius(18),
            Background = Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Brush,
            VerticalAlignment = VerticalAlignment.Center
        };

        iconContainer.Child = new FontIcon
        {
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 64,
            Glyph = payload.IsDirectory ? "\uE8B7" : "\uE7C3",
            Opacity = 0.78,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var stackPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10
        };

        stackPanel.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(payload.Path),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        stackPanel.Children.Add(CreateMetadataRow("\uE7C3", payload.TypeName));
        stackPanel.Children.Add(CreateMetadataRow("\uE9CE", payload.Detail));
        stackPanel.Children.Add(CreateMetadataRow("\uE787", payload.LastWriteTime.ToString("G")));

        Grid.SetColumn(stackPanel, 1);
        grid.Children.Add(iconContainer);
        grid.Children.Add(stackPanel);

        return grid;
    }

    private static UIElement CreateMetadataRow(string glyph, string text)
    {
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        stackPanel.Children.Add(new FontIcon
        {
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 12,
            Glyph = glyph,
            Opacity = 0.55,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 16
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 13.5,
            Opacity = 0.78,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        return stackPanel;
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
        var maxImageWidth = Math.Max(maxWidth - 64, 360);
        var maxImageHeight = Math.Max(maxHeight - 110, 260);
        var imageScale = Math.Min(Math.Min(maxImageWidth / pixelWidth, maxImageHeight / pixelHeight), 1.0);
        var contentWidth = Math.Round(pixelWidth * imageScale);
        var contentHeight = Math.Round(pixelHeight * imageScale);
        var windowWidth = Math.Clamp(contentWidth + 64, 500, maxWidth);
        var windowHeight = Math.Clamp(contentHeight + 110, 360, maxHeight);
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
            return GetCenteredBounds(GetConstrainedWindowSize(720, 460));
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
        var windowWidth = Math.Clamp((longestLineLength * 7.2) + 200, 720, maxWidth);
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
