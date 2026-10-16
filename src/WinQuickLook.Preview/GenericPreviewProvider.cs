using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace WinQuickLook.Preview;

public sealed class GenericPreviewProvider : IPreviewProvider
{
    public bool CanPreview(FileSystemInfo fileSystemInfo)
    {
        return fileSystemInfo.Exists;
    }

    public bool TryCreatePreview(PreviewRequest request, [NotNullWhen(true)] out PreviewResult? result)
    {
        if (Directory.Exists(request.FilePath))
        {
            var directoryInfo = new DirectoryInfo(request.FilePath);

            result = new PreviewResult(
                PreviewKind.Directory,
                directoryInfo.FullName,
                directoryInfo.Name,
                new FileSystemPreviewPayload(directoryInfo.FullName, true, "Directory", GetDirectoryDetail(directoryInfo), directoryInfo.LastWriteTime));

            return true;
        }

        var fileInfo = new FileInfo(request.FilePath);

        if (!fileInfo.Exists)
        {
            result = null;

            return false;
        }

        result = new PreviewResult(
            PreviewKind.Generic,
            fileInfo.FullName,
            fileInfo.Name,
            new FileSystemPreviewPayload(fileInfo.FullName, false, GetFileTypeName(fileInfo), FormatFileSize(fileInfo.Length), fileInfo.LastWriteTime));

        return true;
    }

    private static string GetDirectoryDetail(DirectoryInfo directoryInfo)
    {
        try
        {
            var count = directoryInfo.EnumerateFileSystemInfos().Take(10_001).Count();

            return count > 10_000 ? "10,000+ items" : $"{count:N0} items";
        }
        catch (IOException)
        {
            return "Count unavailable";
        }
        catch (UnauthorizedAccessException)
        {
            return "Count unavailable";
        }
    }

    private static string GetFileTypeName(FileInfo fileInfo)
    {
        return string.IsNullOrEmpty(fileInfo.Extension)
            ? "File"
            : $"{fileInfo.Extension.TrimStart('.').ToUpperInvariant()} file";
    }

    private static string FormatFileSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)size;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0 ? $"{size:N0} {units[unitIndex]}" : $"{value:N1} {units[unitIndex]}";
    }
}
