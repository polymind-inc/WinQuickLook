using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using WinQuickLook.Controls;
using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class MarkdownFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Highest;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        if (!s_supportedExtensions.Contains(fileInfo.Extension, StringComparer.OrdinalIgnoreCase))
        {
            handlerResult = default;

            return false;
        }

        var markdownFileControl = new MarkdownFileControl();

        using (markdownFileControl.Initialize())
        {
            markdownFileControl.Open(fileInfo);
        }

        handlerResult = new HandlerResult { Content = markdownFileControl };

        return true;
    }

    private static readonly IReadOnlyList<string> s_supportedExtensions = [".md", ".markdown"];
}
