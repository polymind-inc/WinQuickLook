using System;
using System.IO;

using WinQuickLook.Preview;

using Xunit;

namespace WinQuickLook.Tests.Preview;

public class ImagePreviewProviderTests
{
    [Theory]
    [InlineData("sample.png")]
    [InlineData("sample.jpg")]
    [InlineData("sample.jpeg")]
    [InlineData("sample.bmp")]
    [InlineData("sample.gif")]
    [InlineData("sample.webp")]
    public void CanPreview_ReturnsTrue_ForSupportedImageExtension(string fileName)
    {
        using var temp = new TemporaryDirectory();
        var fileInfo = temp.CreateFile(fileName);

        var provider = new ImagePreviewProvider();

        Assert.True(provider.CanPreview(fileInfo));
    }

    [Fact]
    public void TryCreatePreview_ReturnsImageResult()
    {
        using var temp = new TemporaryDirectory();
        var fileInfo = temp.CreateFile("sample.png");
        var provider = new ImagePreviewProvider();

        var success = provider.TryCreatePreview(new PreviewRequest(fileInfo.FullName), out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(PreviewKind.Image, result.Kind);
        Assert.Equal(fileInfo.FullName, result.FilePath);
        Assert.IsType<ImagePreviewPayload>(result.Payload);
    }

    [Fact]
    public void CreateWithDirectorySiblings_ReturnsOrderedSupportedImages()
    {
        using var temp = new TemporaryDirectory();
        var first = temp.CreateFile("a.png");
        var current = temp.CreateFile("b.jpg");
        var third = temp.CreateFile("c.webp");
        temp.CreateFile("notes.txt");

        var request = PreviewRequestFactory.CreateWithDirectorySiblings(current.FullName, new ImagePreviewProvider());

        Assert.Equal(current.FullName, request.FilePath);
        Assert.Equal(1, request.CurrentIndex);
        Assert.Equal([first.FullName, current.FullName, third.FullName], request.SiblingFilePaths);
    }

    [Fact]
    public void AggregatePreviewProvider_CanPreview_TextFile()
    {
        using var temp = new TemporaryDirectory();
        var fileInfo = temp.CreateFile("sample.cs", "Console.WriteLine(\"Hello\");");

        var provider = AggregatePreviewProvider.CreateDefault();

        Assert.True(provider.CanPreview(fileInfo));
    }

    [Fact]
    public void TextPreviewProvider_ReturnsTextResult()
    {
        using var temp = new TemporaryDirectory();
        var fileInfo = temp.CreateFile("sample.json", "{\"ok\":true}");
        var provider = new TextPreviewProvider();

        var success = provider.TryCreatePreview(new PreviewRequest(fileInfo.FullName), out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(PreviewKind.Text, result.Kind);
        var payload = Assert.IsType<TextPreviewPayload>(result.Payload);
        Assert.Equal("{\"ok\":true}", payload.Text);
        Assert.Equal("JSON", payload.Language);
    }

    [Fact]
    public void MarkdownPreviewProvider_ReturnsMarkdownResult()
    {
        using var temp = new TemporaryDirectory();
        var fileInfo = temp.CreateFile("readme.md", "# Hello");
        var provider = new MarkdownPreviewProvider();

        var success = provider.TryCreatePreview(new PreviewRequest(fileInfo.FullName), out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(PreviewKind.Markdown, result.Kind);
        var payload = Assert.IsType<WebDocumentPreviewPayload>(result.Payload);
        Assert.Equal(WebDocumentKind.Markdown, payload.Kind);
        Assert.Contains("<h1", payload.Html);
    }

    [Fact]
    public void WebDocumentPreviewProvider_ReturnsPdfResult()
    {
        using var temp = new TemporaryDirectory();
        var fileInfo = temp.CreateFile("sample.pdf");
        var provider = new WebDocumentPreviewProvider();

        var success = provider.TryCreatePreview(new PreviewRequest(fileInfo.FullName), out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(PreviewKind.Pdf, result.Kind);
        var payload = Assert.IsType<WebDocumentPreviewPayload>(result.Payload);
        Assert.Equal(WebDocumentKind.Pdf, payload.Kind);
    }

    [Fact]
    public void MediaPreviewProvider_ReturnsAudioResult()
    {
        using var temp = new TemporaryDirectory();
        var fileInfo = temp.CreateFile("sample.mp3");
        var provider = new MediaPreviewProvider();

        var success = provider.TryCreatePreview(new PreviewRequest(fileInfo.FullName), out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(PreviewKind.Audio, result.Kind);
        var payload = Assert.IsType<MediaPreviewPayload>(result.Payload);
        Assert.False(payload.HasVideo);
    }

    [Fact]
    public void GenericPreviewProvider_ReturnsDirectoryResult()
    {
        using var temp = new TemporaryDirectory();
        temp.CreateFile("child.txt");
        var provider = new GenericPreviewProvider();

        var success = provider.TryCreatePreview(new PreviewRequest(temp.DirectoryPath), out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(PreviewKind.Directory, result.Kind);
        var payload = Assert.IsType<FileSystemPreviewPayload>(result.Payload);
        Assert.True(payload.IsDirectory);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public string DirectoryPath => _path;

        public TemporaryDirectory()
        {
            Directory.CreateDirectory(_path);
        }

        public FileInfo CreateFile(string fileName, string content = "")
        {
            var path = Path.Combine(_path, fileName);

            File.WriteAllText(path, content);

            return new FileInfo(path);
        }

        public void Dispose()
        {
            Directory.Delete(_path, true);
        }
    }
}
