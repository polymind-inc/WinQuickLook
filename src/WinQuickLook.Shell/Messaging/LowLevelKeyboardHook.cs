using System;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinQuickLook.Messaging;

public class LowLevelKeyboardHook() : WindowsHook(WINDOWS_HOOK_ID.WH_KEYBOARD_LL)
{
    public Action<VIRTUAL_KEY>? PerformKeyDown { get; set; }

    protected override LRESULT HookProc(int code, WPARAM wParam, LPARAM lParam)
    {
        if (code == PInvoke.HC_ACTION && wParam == PInvoke.WM_KEYDOWN)
        {
            var kbdllhook = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            PerformKeyDown?.Invoke((VIRTUAL_KEY)kbdllhook.vkCode);
        }

        return base.HookProc(code, wParam, lParam);
    }
}
