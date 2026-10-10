using System;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace WinQuickLook.Preview;

public sealed class TextPreviewProvider : IPreviewProvider
{
    private const int MaxPreviewBytes = 1024 * 1024;

    public static readonly FrozenSet<string> SupportedExtensions = new[]
    {
        ".bat",
        ".c",
        ".cmd",
        ".config",
        ".cpp",
        ".cs",
        ".csproj",
        ".css",
        ".csv",
        ".fs",
        ".fsproj",
        ".go",
        ".h",
        ".hpp",
        ".htm",
        ".html",
        ".ini",
        ".java",
        ".js",
        ".json",
        ".jsx",
        ".kt",
        ".log",
        ".markdown",
        ".md",
        ".mjs",
        ".props",
        ".ps1",
        ".psd1",
        ".psm1",
        ".py",
        ".rb",
        ".rs",
        ".scss",
        ".sh",
        ".sln",
        ".slnx",
        ".sql",
        ".swift",
        ".targets",
        ".ts",
        ".tsx",
        ".txt",
        ".vb",
        ".vbproj",
        ".vue",
        ".xaml",
        ".xml",
        ".yaml",
        ".yml"
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
            var bytes = File.ReadAllBytes(fileInfo.FullName);
            var isTruncated = bytes.Length > MaxPreviewBytes;

            if (isTruncated)
            {
                Array.Resize(ref bytes, MaxPreviewBytes);
            }

            if (ContainsNullByte(bytes))
            {
                result = null;

                return false;
            }

            result = new PreviewResult(
                PreviewKind.Text,
                fileInfo.FullName,
                fileInfo.Name,
                new TextPreviewPayload(fileInfo.FullName, DecodeText(bytes), GetLanguage(fileInfo.Extension), isTruncated));

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

    private static bool ContainsNullByte(byte[] bytes)
    {
        return bytes.Contains((byte)0);
    }

    private static string DecodeText(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    private static string? GetLanguage(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".bat" or ".cmd" => "Batch",
            ".c" or ".h" => "C",
            ".config" or ".props" or ".targets" or ".xml" or ".xaml" or ".csproj" or ".vbproj" or ".fsproj" => "XML",
            ".cpp" or ".hpp" => "C++",
            ".cs" => "C#",
            ".css" or ".scss" => "CSS",
            ".csv" => "CSV",
            ".fs" => "F#",
            ".go" => "Go",
            ".htm" or ".html" or ".vue" => "HTML",
            ".java" => "Java",
            ".js" or ".jsx" or ".mjs" => "JavaScript",
            ".json" => "JSON",
            ".kt" => "Kotlin",
            ".md" or ".markdown" => "Markdown",
            ".ps1" or ".psd1" or ".psm1" => "PowerShell",
            ".py" => "Python",
            ".rb" => "Ruby",
            ".rs" => "Rust",
            ".sh" => "Shell",
            ".sln" or ".slnx" => "Solution",
            ".sql" => "SQL",
            ".swift" => "Swift",
            ".ts" or ".tsx" => "TypeScript",
            ".vb" => "Visual Basic",
            ".yaml" or ".yml" => "YAML",
            _ => null
        };
    }
}
