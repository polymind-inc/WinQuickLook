using System.Runtime.InteropServices;

namespace WinQuickLook.App.WinUI;

internal static class WindowComposition
{
    private const int DWMWA_CLOAK = 13;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWCP_ROUND = 2;

    public static void SetCloaked(nint hwnd, bool cloaked)
    {
        var value = cloaked ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref value, sizeof(int));
    }

    public static void EnableRoundedCorners(nint hwnd)
    {
        var value = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref value, sizeof(int));
    }

    public static void SetImmersiveDarkMode(nint hwnd, bool enabled)
    {
        var value = enabled ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int value, int size);
}
