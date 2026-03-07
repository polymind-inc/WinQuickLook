using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace WinQuickLook.Handlers;

public abstract class DirectoryPreviewHandler : IFileSystemPreviewHandler
{
    public abstract HandlerPriorityClass PriorityClass { get; }

    public bool TryCreateViewer(FileSystemInfo fileSystemInfo, [NotNullWhen(true)] out HandlerResult? handlerResult)
    {
        if (fileSystemInfo is DirectoryInfo directoryInfo)
        {
            return TryCreateViewer(directoryInfo, out handlerResult);
        }

        handlerResult = default;

        return false;
    }

    protected abstract bool TryCreateViewer(DirectoryInfo directoryInfo, out HandlerResult? handlerResult);
}
