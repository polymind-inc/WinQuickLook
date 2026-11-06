using System;

using Windows.Foundation;

namespace WinQuickLook;

public static class PreviewSizing
{
    // Sizes are DIPs, except requestedContent when contentScale is a display's DPI scale.
    public static Size GetClientSize(Size requestedContent, Size workArea, Size chrome, Size minimum, double contentScale = 1)
    {
        var maximumWidth = Math.Max(1, workArea.Width * 0.8);
        var maximumHeight = Math.Max(1, workArea.Height * 0.8);
        var contentWidth = Math.Max(1, requestedContent.Width / contentScale);
        var contentHeight = Math.Max(1, requestedContent.Height / contentScale);
        var scale = Math.Min(1, Math.Min(
            Math.Max(1, maximumWidth - chrome.Width) / contentWidth,
            Math.Max(1, maximumHeight - chrome.Height) / contentHeight));

        return new Size(
            Math.Clamp(Math.Ceiling(contentWidth * scale + chrome.Width), Math.Min(minimum.Width, maximumWidth), maximumWidth),
            Math.Clamp(Math.Ceiling(contentHeight * scale + chrome.Height), Math.Min(minimum.Height, maximumHeight), maximumHeight));
    }
}
