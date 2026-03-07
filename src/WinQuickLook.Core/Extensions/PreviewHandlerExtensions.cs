using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using WinQuickLook.Handlers;

namespace WinQuickLook.Extensions;

public static class PreviewHandlerExtensions
{
    public static bool TryCreateViewer(this IEnumerable<IFileSystemPreviewHandler> previewHandlers, FileSystemInfo fileSystemInfo, [NotNullWhen(true)] out HandlerResult? handlerResult)
    {
        foreach (var previewHandler in previewHandlers)
        {
            if (previewHandler.TryCreateViewer(fileSystemInfo, out handlerResult))
            {
                return true;
            }
        }

        handlerResult = default;

        return false;
    }
}
