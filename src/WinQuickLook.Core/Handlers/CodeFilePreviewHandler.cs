using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class CodeFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.AboveNormal;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        if (!s_languages.TryGetValue(fileInfo.Extension, out var language))
        {
            handlerResult = null;
            return false;
        }

        using var stream = fileInfo.OpenReadNoLock();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();

        handlerResult = new HandlerResult { Content = new TextPreview(fileInfo, text, reader.CurrentEncoding, language) };
        return true;
    }

    private static readonly Dictionary<string, string> s_languages = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "csharp",
        [".csx"] = "csharp",
        [".vb"] = "vb",
        [".fs"] = "fsharp",
        [".fsx"] = "fsharp",
        [".js"] = "javascript",
        [".jsx"] = "javascript",
        [".cjs"] = "javascript",
        [".mjs"] = "javascript",
        [".ts"] = "typescript",
        [".tsx"] = "typescript",
        [".vue"] = "html",
        [".html"] = "html",
        [".htm"] = "html",
        [".xml"] = "xml",
        [".xaml"] = "xml",
        [".xsd"] = "xml",
        [".xsl"] = "xml",
        [".css"] = "css",
        [".scss"] = "css",
        [".less"] = "css",
        [".json"] = "json",
        [".jsonc"] = "json",
        [".c"] = "cpp",
        [".h"] = "cpp",
        [".cpp"] = "cpp",
        [".hpp"] = "cpp",
        [".cc"] = "cpp",
        [".java"] = "java",
        [".py"] = "python",
        [".pyw"] = "python",
        [".rb"] = "ruby",
        [".php"] = "php",
        [".go"] = "go",
        [".rs"] = "rust",
        [".swift"] = "swift",
        [".kt"] = "kotlin",
        [".sql"] = "sql",
        [".ps1"] = "powershell",
        [".psm1"] = "powershell",
        [".psd1"] = "powershell",
        [".sh"] = "bash",
        [".bash"] = "bash",
        [".bat"] = "batch",
        [".cmd"] = "batch",
        [".yaml"] = "yaml",
        [".yml"] = "yaml",
        [".toml"] = "toml",
        [".ini"] = "ini",
        [".config"] = "xml",
        [".tex"] = "latex",
        [".r"] = "r",
        [".lua"] = "lua",
        [".pl"] = "perl"
    };
}
