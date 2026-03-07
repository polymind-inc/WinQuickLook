using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WinQuickLook.Handlers;

public class PdfFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Highest;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        if (!s_supportedExtensions.Contains(fileInfo.Extension, StringComparer.OrdinalIgnoreCase))
        {
            handlerResult = default;

            return false;
        }

        throw new NotImplementedException();
    }

    private static readonly IReadOnlyList<string> s_supportedExtensions = [".pdf"];
}
