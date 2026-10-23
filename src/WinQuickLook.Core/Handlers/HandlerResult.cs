using Windows.Foundation;

namespace WinQuickLook.Handlers;

public class HandlerResult
{
    public PreviewContent Content { get; init; } = null!;

    public Size RequestSize { get; set; } = new(1200, 900);
}
