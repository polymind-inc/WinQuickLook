using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinQuickLook.Messaging;

public class LowLevelMouseHook() : WindowsHook(WINDOWS_HOOK_ID.WH_MOUSE_LL)
{
    protected override LRESULT HookProc(int code, WPARAM wParam, LPARAM lParam)
    {
        if (code == PInvoke.HC_ACTION && wParam == PInvoke.WM_LBUTTONDOWN)
        {

        }

        return base.HookProc(code, wParam, lParam);
    }
}
