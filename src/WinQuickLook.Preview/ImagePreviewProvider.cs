using System;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace WinQuickLook.Preview;

public sealed class ImagePreviewProvider : IPreviewProvider
{
    public static readonly FrozenSet<string> SupportedExtensions = new[]
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
        ".webp"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public bool CanPreview(FileSystemInfo fileSystemInfo)
    {
        return fileSystemInfo is FileInfo fileInfo && fileInfo.Exists && SupportedExtensions.Contains(fileInfo.Extension);
    }

    public bool TryCreatePreview(PreviewRequest request, [NotNullWhen(true)] out PreviewResult? result)
    {
        var fileInfo = new FileInfo(request.FilePath);

        if (!CanPreview(fileInfo))
        {
            result = null;

            return false;
        }

        result = new PreviewResult(
            PreviewKind.Image,
            fileInfo.FullName,
            fileInfo.Name,
            new ImagePreviewPayload(fileInfo.FullName));

        return true;
    }
}
