using Windows.Win32;
using Windows.Win32.Foundation;

namespace WinQuickLook.Extensions;

internal static class NativeStringExtensions
{
    public static string? ToStringAndFree(this PWSTR value)
    {
        try
        {
            return value.ToString();
        }
        finally
        {
            value.FreeCoTaskMem();
        }
    }
}
