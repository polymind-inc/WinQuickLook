using System;

namespace WinQuickLook.Preview;

public sealed record FileSystemPreviewPayload(
    string Path,
    bool IsDirectory,
    string TypeName,
    string Detail,
    DateTime LastWriteTime);
