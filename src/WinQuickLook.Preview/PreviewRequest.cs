using System.Collections.Generic;

namespace WinQuickLook.Preview;

public sealed record PreviewRequest(
    string FilePath,
    IReadOnlyList<string>? SiblingFilePaths = null,
    int CurrentIndex = 0);
