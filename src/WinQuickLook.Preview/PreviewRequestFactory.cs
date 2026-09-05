using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WinQuickLook.Preview;

public static class PreviewRequestFactory
{
    public static PreviewRequest Create(string filePath, IReadOnlyList<string>? siblingFilePaths = null)
    {
        var fullPath = Path.GetFullPath(filePath);

        if (siblingFilePaths is null || siblingFilePaths.Count == 0)
        {
            return new PreviewRequest(fullPath);
        }

        var siblings = siblingFilePaths
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var currentIndex = Array.FindIndex(siblings, x => string.Equals(x, fullPath, StringComparison.OrdinalIgnoreCase));

        if (currentIndex >= 0)
        {
            return new PreviewRequest(fullPath, siblings, currentIndex);
        }

        var expandedSiblings = siblings
            .Append(fullPath)
            .ToArray();

        return new PreviewRequest(fullPath, expandedSiblings, expandedSiblings.Length - 1);
    }

    public static PreviewRequest CreateWithDirectorySiblings(string filePath, IPreviewProvider provider)
    {
        if (Directory.Exists(filePath))
        {
            return Create(filePath);
        }

        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Directory is null || !fileInfo.Directory.Exists)
        {
            return Create(fileInfo.FullName);
        }

        try
        {
            var siblings = fileInfo.Directory
                .EnumerateFiles()
                .Where(provider.CanPreview)
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.FullName, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.FullName)
                .ToArray();

            return Create(fileInfo.FullName, siblings);
        }
        catch (IOException)
        {
            return Create(fileInfo.FullName);
        }
        catch (UnauthorizedAccessException)
        {
            return Create(fileInfo.FullName);
        }
    }
}
