using System.IO;

namespace WinQuickLook.Extensions;

internal static class FileInfoExtensions
{
    public static FileStream OpenReadNoLock(this FileInfo fileInfo) => fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
}
