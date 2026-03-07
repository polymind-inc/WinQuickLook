using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using WinQuickLook.Controls;
using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class HtmlFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Highest;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        if (!s_supportedExtensions.Contains(fileInfo.Extension, StringComparer.OrdinalIgnoreCase))
        {
            handlerResult = default;

            return false;
        }

        var htmlFileControl = new HtmlFileControl();

        using (htmlFileControl.Initialize())
        {
            htmlFileControl.Open(fileInfo);
        }

        handlerResult = new HandlerResult { Content = htmlFileControl };

        return true;
    }

    private static readonly IReadOnlyList<string> s_supportedExtensions = [".htm", ".html", ".xhtml"];
}
