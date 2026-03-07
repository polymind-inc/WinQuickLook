using System.IO;

using ICSharpCode.AvalonEdit.Highlighting;

using Markdig;

using Microsoft.Web.WebView2.Core;

using WinQuickLook.Extensions;

namespace WinQuickLook.Controls;

public partial class MarkdownFileControl
{
    public MarkdownFileControl() => InitializeComponent();

    public async void Open(FileInfo fileInfo)
    {
        textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("MarkDown");
        textEditor.Load(fileInfo.OpenReadNoLock());

        await webView2.EnsureCoreWebView2Async();
    }

    private void WebView2_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        var pipeline = new MarkdownPipelineBuilder()
                       .UseAdvancedExtensions()
                       .Build();

        webView2.NavigateToString(Markdown.ToHtml(textEditor.Text, pipeline));
    }
}
