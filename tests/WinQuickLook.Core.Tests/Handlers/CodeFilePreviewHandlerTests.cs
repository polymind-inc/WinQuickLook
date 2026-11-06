using System;
using System.IO;



using WinQuickLook.Handlers;

using Xunit;

namespace WinQuickLook.Tests.Handlers;

public class CodeFilePreviewHandlerTests
{
    [StaTheory]
    [InlineData("test.cs")]
    [InlineData("test.ts")]
    [InlineData("test.tsx")]
    [InlineData("test.vue")]
    [InlineData("test.cjs")]
    [InlineData("test.mjs")]
    public void TryCreateViewer_SupportedCodeExtension_ReturnsTrue(string fileName)
    {
        using var temporaryFile = TemporaryFile.Create(fileName, "const value = 1;");

        var handler = new CodeFilePreviewHandler();

        var actual = handler.TryCreateViewer(temporaryFile.FileInfo, out _);

        Assert.True(actual);
    }

    [StaTheory]
    [InlineData("test.txt")]
    [InlineData("test")]
    public void TryCreateViewer_UnsupportedCodeExtension_ReturnsFalse(string fileName)
    {
        using var temporaryFile = TemporaryFile.Create(fileName, "plain text");

        var handler = new CodeFilePreviewHandler();

        var actual = handler.TryCreateViewer(temporaryFile.FileInfo, out var handlerResult);

        Assert.False(actual);
        Assert.Null(handlerResult);
    }

    [StaFact]
    public void TryCreateViewer_SupportedCodeFile_ReturnsTextWithLanguage()
    {
        using var temporaryFile = TemporaryFile.Create("test.ts", "const value = 1;");

        var handler = new CodeFilePreviewHandler();

        var actual = handler.TryCreateViewer(temporaryFile.FileInfo, out var handlerResult);

        Assert.True(actual);
        Assert.NotNull(handlerResult);

        var textEditor = Assert.IsType<TextPreview>(handlerResult.Content);
        Assert.Equal("typescript", textEditor.Language);
    }

    private sealed class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path)
        {
            Path = path;
            FileInfo = new FileInfo(path);
        }

        public string Path { get; }

        public FileInfo FileInfo { get; }

        public static TemporaryFile Create(string fileName, string content)
        {
            var directoryPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(directoryPath);

            var path = System.IO.Path.Combine(directoryPath, fileName);

            File.WriteAllText(path, content);

            return new TemporaryFile(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(System.IO.Path.GetDirectoryName(Path)))
            {
                Directory.Delete(System.IO.Path.GetDirectoryName(Path)!, true);
            }
        }
    }
}
