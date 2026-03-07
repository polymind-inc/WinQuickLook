using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace Windows.Win32.UI.Shell;

[Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport()]
public interface IShellWindows
{
    /// <summary>Gets the number of windows in the Shell windows collection.</summary>
    /// <param name="Count">
    /// <para>Type: <b>long*</b> The number of windows in the Shell windows collection.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-get_count#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns <b xmlns:loc="http://microsoft.com/wdcml/l10n">S_OK</b>. Otherwise, it returns an <b xmlns:loc="http://microsoft.com/wdcml/l10n">HRESULT</b> error code.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-get_count">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    unsafe Foundation.HRESULT get_Count(int* Count);
    /// <summary>Returns the registered Shell window for a specified index.</summary>
    /// <param name="index">
    /// <para>Type: <b>VARIANT</b> A <a href="https://docs.microsoft.com/windows/desktop/api/oaidl/ns-oaidl-variant">VARIANT</a> of type VT_UI4, VT_I2, or VT_I4. If the type is VT_UI4, the value of <i>index</i> is interpreted as a member of <a href="https://docs.microsoft.com/windows/desktop/api/exdisp/ne-exdisp-shellwindowtypeconstants">ShellWindowTypeConstants</a>; in this case, <b>Item</b> returns the window that is closest to the foreground window and has a matching type. If the type is VT_I, or VT_I4, <i>index</i> is treated as an index into the Shell windows collection.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-item#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="Folder">
    /// <para>Type: <b><a href="https://docs.microsoft.com/previous-versions/windows/desktop/api/oaidl/nn-oaidl-idispatch">IDispatch</a>**</b> A reference to the window's <a href="https://docs.microsoft.com/previous-versions/windows/desktop/api/oaidl/nn-oaidl-idispatch">IDispatch</a> interface, or <b>NULL</b> if the specified window was not found.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-item#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> One of the following values, or a standard result code. </para>
    /// <para>This doc was truncated.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-item">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    Foundation.HRESULT Item([MarshalAs(UnmanagedType.Struct)] object index, [MarshalAs(UnmanagedType.IDispatch)] out object Folder);
    /// <summary>Retrieves an enumerator for the collection of Shell windows.</summary>
    /// <param name="ppunk">
    /// <para>Type: <b><a href="https://docs.microsoft.com/windows/desktop/api/unknwn/nn-unknwn-iunknown">IUnknown</a>**</b> When this method returns, contains an interface pointer to an object that implements the <a href="https://docs.microsoft.com/previous-versions/windows/desktop/api/oaidl/nn-oaidl-ienumvariant">IEnumVARIANT</a> interface.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-_newenum#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns <b xmlns:loc="http://microsoft.com/wdcml/l10n">S_OK</b>. Otherwise, it returns an <b xmlns:loc="http://microsoft.com/wdcml/l10n">HRESULT</b> error code.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-_newenum">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    Foundation.HRESULT _NewEnum([MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
    /// <summary>Registers an open window as a Shell window; the window is specified by handle.</summary>
    /// <param name="pid">
    /// <para>Type: <b><a href="https://docs.microsoft.com/previous-versions/windows/desktop/api/oaidl/nn-oaidl-idispatch">IDispatch</a>*</b> The window's <a href="https://docs.microsoft.com/previous-versions/windows/desktop/api/oaidl/nn-oaidl-idispatch">IDispatch</a> interface.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-register#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="hwnd">
    /// <para>Type: <b>long</b> A handle that specifies the window to register.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-register#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="swClass">
    /// <para>Type: <b>int</b> A member of <a href="https://docs.microsoft.com/windows/desktop/api/exdisp/ne-exdisp-shellwindowtypeconstants">ShellWindowTypeConstants</a> that specifies the type of window.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-register#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="plCookie">
    /// <para>Type: <b>long*</b> The window's cookie.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-register#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns <b xmlns:loc="http://microsoft.com/wdcml/l10n">S_OK</b>. Otherwise, it returns an <b xmlns:loc="http://microsoft.com/wdcml/l10n">HRESULT</b> error code.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-register">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    unsafe Foundation.HRESULT Register([MarshalAs(UnmanagedType.IDispatch)] object pid, int hwnd, int swClass, int* plCookie);
    /// <summary>Registers a pending window as a Shell window; the window is specified by an absolute PIDL.</summary>
    /// <param name="lThreadId">A thread ID.</param>
    /// <param name="pvarloc">
    /// <para>Type: <b>VARIANT*</b> A <a href="https://docs.microsoft.com/windows/desktop/api/oaidl/ns-oaidl-variant">VARIANT</a> of type VT_VARIANT | VT_BYREF. Set the value of <i>pvarloc</i> to an absolute <a href="https://docs.microsoft.com/windows/desktop/api/shtypes/ns-shtypes-itemidlist">PIDL</a> (PIDLIST_ABSOLUTE) that specifies the window to register.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-registerpending#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="pvarlocRoot">
    /// <para>Type: <b>VARIANT*</b> Must be <b>NULL</b> or of type VT_EMPTY.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-registerpending#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="swClass">
    /// <para>Type: <b>int</b> A member of <a href="https://docs.microsoft.com/windows/desktop/api/exdisp/ne-exdisp-shellwindowtypeconstants">ShellWindowTypeConstants</a> that specifies the type of window.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-registerpending#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="plCookie">
    /// <para>Type: <b>long*</b> The window's cookie.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-registerpending#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns <b xmlns:loc="http://microsoft.com/wdcml/l10n">S_OK</b>. Otherwise, it returns an <b xmlns:loc="http://microsoft.com/wdcml/l10n">HRESULT</b> error code.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-registerpending">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    unsafe Foundation.HRESULT RegisterPending(int lThreadId, [MarshalAs(UnmanagedType.Struct)] in object pvarloc, [MarshalAs(UnmanagedType.Struct)] in object pvarlocRoot, int swClass, int* plCookie);
    /// <summary>Revokes a Shell window's registration and removes the window from the Shell windows collection.</summary>
    /// <param name="lCookie">
    /// <para>Type: <b>long*</b> The cookie that identifies the window to un-register.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-revoke#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns <b xmlns:loc="http://microsoft.com/wdcml/l10n">S_OK</b>. Otherwise, it returns an <b xmlns:loc="http://microsoft.com/wdcml/l10n">HRESULT</b> error code.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-revoke">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    Foundation.HRESULT Revoke(int lCookie);
    /// <summary>Occurs when a Shell window is navigated to a new location.</summary>
    /// <param name="lCookie">
    /// <para>Type: <b>long</b> The cookie that identifies the window.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-onnavigate#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="pvarLoc">
    /// <para>Type: <b>VARIANT*</b> A <a href="https://docs.microsoft.com/windows/desktop/api/oaidl/ns-oaidl-variant">VARIANT</a> of type VT_VARIANT | VT_BYREF. Set the value of <i>pvarLoc</i> to an absolute <a href="https://docs.microsoft.com/windows/desktop/api/shtypes/ns-shtypes-itemidlist">PIDL</a> (PIDLIST_ABSOLUTE) that specifies the new location.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-onnavigate#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns <b xmlns:loc="http://microsoft.com/wdcml/l10n">S_OK</b>. Otherwise, it returns an <b xmlns:loc="http://microsoft.com/wdcml/l10n">HRESULT</b> error code.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-onnavigate">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    Foundation.HRESULT OnNavigate(int lCookie, [MarshalAs(UnmanagedType.Struct)] in object pvarLoc);
    /// <summary>Occurs when a Shell window's activation state changes.</summary>
    /// <param name="lCookie">
    /// <para>Type: <b>long</b> The cookie that identifies the window.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-onactivated#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="fActive">
    /// <para>Type: <b>VARIANT_BOOL</b> <b>TRUE</b> if the window is being activated; <b>FALSE</b> if the window is being deactivated.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-onactivated#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns <b xmlns:loc="http://microsoft.com/wdcml/l10n">S_OK</b>. Otherwise, it returns an <b xmlns:loc="http://microsoft.com/wdcml/l10n">HRESULT</b> error code.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-onactivated">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    Foundation.HRESULT OnActivated(int lCookie, short fActive);
    /// <summary>Finds a window in the Shell windows collection and returns the window's handle and IDispatch interface.</summary>
    /// <param name="pvarLoc">
    /// <para>Type: <b>VARIANT*</b> A <a href="https://docs.microsoft.com/windows/desktop/api/oaidl/ns-oaidl-variant">VARIANT</a> of type VT_VARIANT | VT_BYREF. Set the value of <i>pvarLoc</i> to an absolute <a href="https://docs.microsoft.com/windows/desktop/api/shtypes/ns-shtypes-itemidlist">PIDL</a> (PIDLIST_ABSOLUTE) that specifies the window to find. (See remarks.)</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-findwindowsw#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="pvarLocRoot">
    /// <para>Type: <b>VARIANT*</b> Must be <b>NULL</b> or of type VT_EMPTY.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-findwindowsw#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="swClass">
    /// <para>Type: <b>int</b> One or more <a href="https://docs.microsoft.com/windows/desktop/api/exdisp/ne-exdisp-shellwindowtypeconstants">ShellWindowTypeConstants</a> flags that specify window types to include in the search.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-findwindowsw#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="phwnd">
    /// <para>Type: <b>long*</b> A handle for the window matching the specified search criteria, or <b>NULL</b> if no such window was found.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-findwindowsw#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="swfwOptions">
    /// <para>Type: <b>int</b> One or more <a href="https://docs.microsoft.com/windows/desktop/api/exdisp/ne-exdisp-shellwindowfindwindowoptions">ShellWindowFindWindowOptions</a> flags that specify search options.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-findwindowsw#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="ppdispOut">
    /// <para>Type: <b><a href="https://docs.microsoft.com/previous-versions/windows/desktop/api/oaidl/nn-oaidl-idispatch">IDispatch</a>**</b> A reference to the window's <a href="https://docs.microsoft.com/previous-versions/windows/desktop/api/oaidl/nn-oaidl-idispatch">IDispatch</a> interface, or <b>NULL</b> if no such window was found.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-findwindowsw#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> One of the following values, or a standard result code. </para>
    /// <para>This doc was truncated.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-findwindowsw">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    unsafe Foundation.HRESULT FindWindowSW([MarshalAs(UnmanagedType.Struct)] in object pvarLoc, [MarshalAs(UnmanagedType.Struct)] in object pvarLocRoot, int swClass, int* phwnd, int swfwOptions, [MarshalAs(UnmanagedType.IDispatch)] out object ppdispOut);
    /// <summary>Occurs when a new Shell window is created for a frame.</summary>
    /// <param name="lCookie">
    /// <para>Type: <b>long</b> The cookie that identifies the window.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-oncreated#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <param name="punk">
    /// <para>Type: <b><a href="https://docs.microsoft.com/windows/desktop/api/unknwn/nn-unknwn-iunknown">IUnknown</a>*</b> The address of the new window's <a href="https://docs.microsoft.com/windows/desktop/api/unknwn/nn-unknwn-iunknown">IUnknown</a> interface.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-oncreated#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns <b xmlns:loc="http://microsoft.com/wdcml/l10n">S_OK</b>. Otherwise, it returns an <b xmlns:loc="http://microsoft.com/wdcml/l10n">HRESULT</b> error code.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-oncreated">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    Foundation.HRESULT OnCreated(int lCookie, [MarshalAs(UnmanagedType.IUnknown)] object punk);
    /// <summary>Deprecated. Always returns S_OK.</summary>
    /// <param name="fAttach">
    /// <para>Type: <b>VARIANT_BOOL</b> Not used.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-processattachdetach#parameters">Read more on docs.microsoft.com</see>.</para>
    /// </param>
    /// <returns>
    /// <para>Type: <b>HRESULT</b> Always returns S_OK.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-ishellwindows-processattachdetach">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    Foundation.HRESULT ProcessAttachDetach(short fAttach);
}
