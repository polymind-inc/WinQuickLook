using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace WinQuickLook.Tests.Interop;

internal unsafe class ShellWindowsStub : IShellWindows
{
    public int Count { get; set; } = 1;
    public HRESULT CountResult { get; set; }
    public HRESULT ItemResult { get; set; } = new(1);
    public object? ItemValue { get; set; }
    public int NativeHwnd { get; set; }

    public HRESULT get_Count(int* count)
    {
        *count = Count;

        return CountResult;
    }

    public HRESULT Item(object index, out object folder)
    {
        folder = ItemValue!;

        return ItemResult;
    }

    public HRESULT FindWindowSW(in object location, in object root, int windowClass, int* hwnd, int options, out object dispatch)
    {
        *hwnd = NativeHwnd;
        dispatch = this;

        return default;
    }

    public HRESULT _NewEnum(out object enumerator) { enumerator = null!; return default; }
    public HRESULT Register(object dispatch, int hwnd, int windowClass, int* cookie) => default;
    public HRESULT RegisterPending(int thread, in object location, in object root, int windowClass, int* cookie) => default;
    public HRESULT Revoke(int cookie) => default;
    public HRESULT OnNavigate(int cookie, in object location) => default;
    public HRESULT OnActivated(int cookie, short active) => default;
    public HRESULT OnCreated(int cookie, object unknown) => default;
    public HRESULT ProcessAttachDetach(short attach) => default;
}
