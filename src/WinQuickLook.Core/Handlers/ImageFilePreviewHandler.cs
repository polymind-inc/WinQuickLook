using System;
using System.IO;

using Windows.Foundation;
using Windows.Graphics.Imaging;

using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class ImageFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Normal;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        try
        {
            using var stream = fileInfo.OpenReadNoLock();
            using var randomAccess = stream.AsRandomAccessStream();
            var decoder = BitmapDecoder.CreateAsync(randomAccess).AsTask().GetAwaiter().GetResult();
            var size = new Size(decoder.OrientedPixelWidth, decoder.OrientedPixelHeight);
            stream.Position = 0;
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            handlerResult = new HandlerResult
            {
                Content = new ImagePreview(fileInfo, buffer.ToArray()),
                RequestSize = size.FitTo(1200)
            };
            return true;
        }
        catch
        {
            handlerResult = null;
            return false;
        }
    }
}
