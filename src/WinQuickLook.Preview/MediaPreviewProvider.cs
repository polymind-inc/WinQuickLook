using System;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace WinQuickLook.Preview;

public sealed class MediaPreviewProvider : IPreviewProvider
{
    public static readonly FrozenSet<string> VideoExtensions = new[]
    {
        ".avi",
        ".m4v",
        ".mkv",
        ".mov",
        ".mp4",
        ".mpeg",
        ".mpg",
        ".webm",
        ".wmv"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> AudioExtensions = new[]
    {
        ".aac",
        ".flac",
        ".m4a",
        ".mp3",
        ".ogg",
        ".wav",
        ".wma"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public bool CanPreview(FileSystemInfo fileSystemInfo)
    {
        return fileSystemInfo is FileInfo fileInfo
            && fileInfo.Exists
            && (VideoExtensions.Contains(fileInfo.Extension) || AudioExtensions.Contains(fileInfo.Extension));
    }

    public bool TryCreatePreview(PreviewRequest request, [NotNullWhen(true)] out PreviewResult? result)
    {
        var fileInfo = new FileInfo(request.FilePath);

        if (!CanPreview(fileInfo))
        {
            result = null;

            return false;
        }

        var hasVideo = VideoExtensions.Contains(fileInfo.Extension);

        result = new PreviewResult(
            hasVideo ? PreviewKind.Video : PreviewKind.Audio,
            fileInfo.FullName,
            fileInfo.Name,
            new MediaPreviewPayload(fileInfo.FullName, hasVideo));

        return true;
    }
}
