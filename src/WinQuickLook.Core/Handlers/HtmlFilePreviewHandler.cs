using System;
using System.IO;
using System.Linq;

using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class HtmlFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Highest;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        if (!s_supportedExtensions.Contains(fileInfo.Extension, StringComparer.OrdinalIgnoreCase))
        {
            handlerResult = null;
            return false;
        }

        using var stream = fileInfo.OpenReadNoLock();
        using var reader = new StreamReader(stream);
        handlerResult = new HandlerResult { Content = new HtmlPreview(fileInfo, reader.ReadToEnd()) };
        return true;
    }

    private static readonly string[] s_supportedExtensions = [".htm", ".html", ".xhtml"];
}
