namespace WinQuickLook.Preview;

public sealed record WebDocumentPreviewPayload(
    string FilePath,
    WebDocumentKind Kind,
    string? Html = null);
