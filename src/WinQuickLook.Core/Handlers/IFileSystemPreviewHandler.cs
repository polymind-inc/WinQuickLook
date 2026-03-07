using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace WinQuickLook.Handlers;

public interface IFileSystemPreviewHandler
{
    HandlerPriorityClass PriorityClass { get; }
    bool TryCreateViewer(FileSystemInfo fileSystemInfo, [NotNullWhen(true)] out HandlerResult? handlerResult);
}
