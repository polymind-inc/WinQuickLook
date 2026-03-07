using System.IO;

using ICSharpCode.AvalonEdit.Highlighting;

using WinQuickLook.Extensions;

namespace WinQuickLook.Controls;

public partial class HtmlFileControl
{
    public HtmlFileControl() => InitializeComponent();

    public void Open(FileInfo fileInfo)
    {
        textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(fileInfo.Extension);
        textEditor.Load(fileInfo.OpenReadNoLock());

        webView2.Source = new System.Uri(fileInfo.FullName);
    }
}
