using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace WinQuickLook.Preview;

public interface IPreviewProvider
{
    bool CanPreview(FileSystemInfo fileSystemInfo);

    bool TryCreatePreview(PreviewRequest request, [NotNullWhen(true)] out PreviewResult? result);
}
