using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace WinQuickLook.Preview;

public sealed class WebDocumentPreviewProvider : IPreviewProvider
{
    private static readonly FrozenDictionary<string, WebDocumentKind> s_supportedExtensions = new Dictionary<string, WebDocumentKind>(StringComparer.OrdinalIgnoreCase)
    {
        [".htm"] = WebDocumentKind.Html,
        [".html"] = WebDocumentKind.Html,
        [".xhtml"] = WebDocumentKind.Html,
        [".pdf"] = WebDocumentKind.Pdf,
        [".svg"] = WebDocumentKind.Svg
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public bool CanPreview(FileSystemInfo fileSystemInfo)
    {
        return fileSystemInfo is FileInfo fileInfo && fileInfo.Exists && s_supportedExtensions.ContainsKey(fileInfo.Extension);
    }

    public bool TryCreatePreview(PreviewRequest request, [NotNullWhen(true)] out PreviewResult? result)
    {
        var fileInfo = new FileInfo(request.FilePath);

        if (!CanPreview(fileInfo) || !s_supportedExtensions.TryGetValue(fileInfo.Extension, out var kind))
        {
            result = null;

            return false;
        }

        result = new PreviewResult(
            GetPreviewKind(kind),
            fileInfo.FullName,
            fileInfo.Name,
            new WebDocumentPreviewPayload(fileInfo.FullName, kind));

        return true;
    }

    private static PreviewKind GetPreviewKind(WebDocumentKind kind)
    {
        return kind switch
        {
            WebDocumentKind.Pdf => PreviewKind.Pdf,
            _ => PreviewKind.Web
        };
    }
}
