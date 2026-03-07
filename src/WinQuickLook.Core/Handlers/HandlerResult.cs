using System.Windows;

namespace WinQuickLook.Handlers;

public class HandlerResult
{
    public FrameworkElement Content { get; init; } = null!;

    public Size RequestSize { get; set; } = s_defaultRequestSize;

    private static readonly Size s_defaultRequestSize = new(1200, 900);
}
