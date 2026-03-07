using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class ImageFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Normal;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        if (!TryGetImageSize(fileInfo, out var imageSize))
        {
            handlerResult = default;

            return false;
        }

        var requestSize = imageSize.FitTo(1200);

        var bitmap = LoadImage(fileInfo, requestSize);

        var image = new Image();

        using (image.Initialize())
        {
            image.Stretch = Stretch.Uniform;
            image.StretchDirection = StretchDirection.DownOnly;
            image.Source = bitmap;
        }

        handlerResult = new HandlerResult { Content = image, RequestSize = requestSize };

        return true;
    }

    private static BitmapImage LoadImage(FileInfo fileInfo, Size decodeSize)
    {
        using var stream = fileInfo.OpenReadNoLock();

        var bitmap = new BitmapImage();

        using (bitmap.Initialize())
        {
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = (int)decodeSize.Width;
            bitmap.DecodePixelHeight = (int)decodeSize.Height;
            bitmap.StreamSource = stream;
        }

        return bitmap;
    }

    private static bool TryGetImageSize(FileInfo fileInfo, out Size size)
    {
        try
        {
            using var stream = fileInfo.OpenReadNoLock();

            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);

            size = new Size(decoder.Frames[0].Width, decoder.Frames[0].Height);

            return true;
        }
        catch
        {
            size = Size.Empty;

            return false;
        }
    }
}
