namespace WinQuickLook.Preview;

public sealed record PreviewResult(
    PreviewKind Kind,
    string FilePath,
    string? Title = null,
    object? Payload = null);
