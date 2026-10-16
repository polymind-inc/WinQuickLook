namespace WinQuickLook.Preview;

public sealed record TextPreviewPayload(
    string FilePath,
    string Text,
    string? Language = null,
    bool IsTruncated = false);
