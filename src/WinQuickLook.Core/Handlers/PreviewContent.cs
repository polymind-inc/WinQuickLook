using System.IO;
using System.Text;

namespace WinQuickLook.Handlers;

public abstract record PreviewContent(FileSystemInfo FileSystemInfo);

public sealed record TextPreview(FileInfo File, string Text, Encoding Encoding, string? Language = null) : PreviewContent(File);

public sealed record HtmlPreview(FileInfo File, string Source, string? Html = null) : PreviewContent(File);

public sealed record ImagePreview(FileInfo File, byte[] Data) : PreviewContent(File);

public sealed record MediaPreview(FileInfo File, bool IsVideo) : PreviewContent(File);

public sealed record ShellPreview(FileInfo File) : PreviewContent(File);

public sealed record GenericPreview(FileSystemInfo File) : PreviewContent(File);
