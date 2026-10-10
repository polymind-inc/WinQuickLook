using System;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Markdig;

namespace WinQuickLook.Preview;

public sealed class MarkdownPreviewProvider : IPreviewProvider
{
    private static readonly MarkdownPipeline s_pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static readonly FrozenSet<string> SupportedExtensions = new[]
    {
        ".md",
        ".markdown"
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

        try
        {
            var markdown = File.ReadAllText(fileInfo.FullName);
            var html = Markdown.ToHtml(markdown, s_pipeline);

            result = new PreviewResult(
                PreviewKind.Markdown,
                fileInfo.FullName,
                fileInfo.Name,
                new WebDocumentPreviewPayload(fileInfo.FullName, WebDocumentKind.Markdown, CreateHtmlDocument(html)));

            return true;
        }
        catch (IOException)
        {
            result = null;

            return false;
        }
        catch (UnauthorizedAccessException)
        {
            result = null;

            return false;
        }
    }

    private static string CreateHtmlDocument(string body)
    {
        return $$"""
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8">
              <style>
                :root { color-scheme: light dark; }
                body {
                  font-family: "Segoe UI", sans-serif;
                  font-size: 15px;
                  line-height: 1.6;
                  margin: 28px 34px;
                }
                pre, code { font-family: "Cascadia Mono", Consolas, monospace; }
                pre {
                  overflow: auto;
                  padding: 14px;
                  border-radius: 8px;
                  background: rgba(127, 127, 127, 0.14);
                }
                img { max-width: 100%; }
                table { border-collapse: collapse; }
                th, td { border: 1px solid rgba(127, 127, 127, 0.35); padding: 6px 10px; }
              </style>
            </head>
            <body>
            {{body}}
            </body>
            </html>
            """;
    }
}
