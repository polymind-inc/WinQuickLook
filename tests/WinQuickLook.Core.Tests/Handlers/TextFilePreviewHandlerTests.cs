using System;
using System.IO;
using System.Text;

using ICSharpCode.AvalonEdit;

using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class TextFilePreviewHandlerTests
{
    [WpfTheory]
    [InlineData(932)]
    [InlineData(65001)]
    [InlineData(1200)]
    public void TryCreateViewer_JapaneseText_DecodesDetectedEncoding(int codePage)
    {
        const string text = "これは日本語のテキストです。文字コードの判定結果が正しく利用されているかを確認します。\r\n";
        var encoding = CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.GetEncoding(codePage);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

        try
        {
            File.WriteAllText(path, text, encoding);

            var handler = new TextFilePreviewHandler();

            var actual = handler.TryCreateViewer(new FileInfo(path), out var handlerResult);

            Assert.True(actual);
            Assert.NotNull(handlerResult);

            var textEditor = Assert.IsType<TextEditor>(handlerResult.Content);

            Assert.Equal(text, textEditor.Text);
            Assert.Equal(codePage, textEditor.Encoding.CodePage);
            Assert.False(textEditor.IsModified);

            using var exclusiveStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
