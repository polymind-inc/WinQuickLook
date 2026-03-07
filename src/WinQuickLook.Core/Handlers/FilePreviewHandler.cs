using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace WinQuickLook.Handlers;

public abstract class FilePreviewHandler : IFileSystemPreviewHandler
{
    public abstract HandlerPriorityClass PriorityClass { get; }

    public bool TryCreateViewer(FileSystemInfo fileSystemInfo, [NotNullWhen(true)] out HandlerResult? handlerResult)
    {
        if (fileSystemInfo is FileInfo fileInfo)
        {
            if (PriorityClass == HandlerPriorityClass.Generic)
            {
                return TryCreateViewer(fileInfo, out handlerResult);
            }

            if (!s_ignoreFileExtensions.Contains(fileInfo.Extension, StringComparer.OrdinalIgnoreCase))
            {
                return TryCreateViewer(fileInfo, out handlerResult);
            }
        }

        handlerResult = default;

        return false;
    }

    protected abstract bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult);

    private static readonly IReadOnlyList<string> s_ignoreFileExtensions =
    [
        ".acm",
        ".ax",
        ".cpl",
        ".dll",
        ".drv",
        ".efi",
        ".exe",
        ".fon",
        ".mui",
        ".ocx",
        ".scr",
        ".sys",
        ".tsp",
        ".appx",
        ".msix",
        ".msixbundle",
        ".msixupload"
    ];
}
