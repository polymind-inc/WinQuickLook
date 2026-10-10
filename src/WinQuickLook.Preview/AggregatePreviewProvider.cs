using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace WinQuickLook.Preview;

public sealed class AggregatePreviewProvider : IPreviewProvider
{
    private readonly IReadOnlyList<IPreviewProvider> _previewProviders;

    public AggregatePreviewProvider(IEnumerable<IPreviewProvider> previewProviders)
    {
        _previewProviders = previewProviders.ToArray();
    }

    public static AggregatePreviewProvider CreateDefault()
    {
        return new AggregatePreviewProvider(
        [
            new ImagePreviewProvider(),
            new MarkdownPreviewProvider(),
            new WebDocumentPreviewProvider(),
            new MediaPreviewProvider(),
            new TextPreviewProvider(),
            new GenericPreviewProvider()
        ]);
    }

    public bool CanPreview(FileSystemInfo fileSystemInfo)
    {
        return _previewProviders.Any(x => x.CanPreview(fileSystemInfo));
    }

    public bool TryCreatePreview(PreviewRequest request, [NotNullWhen(true)] out PreviewResult? result)
    {
        foreach (var previewProvider in _previewProviders)
        {
            if (previewProvider.TryCreatePreview(request, out result))
            {
                return true;
            }
        }

        result = null;

        return false;
    }
}
