using System;
using System.IO;
using System.Linq;

using Markdig;

using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class MarkdownFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Highest;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        if (!s_supportedExtensions.Contains(fileInfo.Extension, StringComparer.OrdinalIgnoreCase))
        {
            handlerResult = null;
            return false;
        }

        using var stream = fileInfo.OpenReadNoLock();
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();
        handlerResult = new HandlerResult { Content = new HtmlPreview(fileInfo, source, Markdown.ToHtml(source, s_pipeline)) };
        return true;
    }

    private static readonly string[] s_supportedExtensions = [".md", ".markdown"];
    private static readonly MarkdownPipeline s_pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
}
