using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace Windows.Win32.UI.Shell;

[Guid("0002DF05-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport()]
public interface IWebBrowserApp
{
    [PreserveSig()]
    new Foundation.HRESULT GoBack();
    [PreserveSig()]
    new Foundation.HRESULT GoForward();
    [PreserveSig()]
    new Foundation.HRESULT GoHome();
    [PreserveSig()]
    new Foundation.HRESULT GoSearch();
    [PreserveSig()]
    new Foundation.HRESULT Navigate(Foundation.BSTR URL, [MarshalAs(UnmanagedType.Struct)] in object Flags, [MarshalAs(UnmanagedType.Struct)] in object TargetFrameName, [MarshalAs(UnmanagedType.Struct)] in object PostData, [MarshalAs(UnmanagedType.Struct)] in object Headers);
    [PreserveSig()]
    new Foundation.HRESULT Refresh();
    [PreserveSig()]
    new Foundation.HRESULT Refresh2([MarshalAs(UnmanagedType.Struct)] in object Level);
    [PreserveSig()]
    new Foundation.HRESULT Stop();
    [PreserveSig()]
    new Foundation.HRESULT get_Application([MarshalAs(UnmanagedType.IDispatch)] out object ppDisp);
    [PreserveSig()]
    new Foundation.HRESULT get_Parent([MarshalAs(UnmanagedType.IDispatch)] out object ppDisp);
    [PreserveSig()]
    new Foundation.HRESULT get_Container([MarshalAs(UnmanagedType.IDispatch)] out object ppDisp);
    [PreserveSig()]
    new Foundation.HRESULT get_Document([MarshalAs(UnmanagedType.IDispatch)] out object ppDisp);
    [PreserveSig()]
    new unsafe Foundation.HRESULT get_TopLevelContainer(short* pBool);
    [PreserveSig()]
    new unsafe Foundation.HRESULT get_Type(Foundation.BSTR* Type);
    [PreserveSig()]
    new unsafe Foundation.HRESULT get_Left(int* pl);
    [PreserveSig()]
    new Foundation.HRESULT put_Left(int Left);
    [PreserveSig()]
    new unsafe Foundation.HRESULT get_Top(int* pl);
    [PreserveSig()]
    new Foundation.HRESULT put_Top(int Top);
    [PreserveSig()]
    new unsafe Foundation.HRESULT get_Width(int* pl);
    [PreserveSig()]
    new Foundation.HRESULT put_Width(int Width);
    [PreserveSig()]
    new unsafe Foundation.HRESULT get_Height(int* pl);
    [PreserveSig()]
    new Foundation.HRESULT put_Height(int Height);
    [PreserveSig()]
    new unsafe Foundation.HRESULT get_LocationName(Foundation.BSTR* LocationName);
    [PreserveSig()]
    new unsafe Foundation.HRESULT get_LocationURL(Foundation.BSTR* LocationURL);
    [PreserveSig()]
    new unsafe Foundation.HRESULT get_Busy(short* pBool);
    [PreserveSig()]
    Foundation.HRESULT Quit();
    [PreserveSig()]
    unsafe Foundation.HRESULT ClientToWindow(int* pcx, int* pcy);
    [PreserveSig()]
    Foundation.HRESULT PutProperty(Foundation.BSTR Property, [MarshalAs(UnmanagedType.Struct)] object vtValue);
    [PreserveSig()]
    Foundation.HRESULT GetProperty(Foundation.BSTR Property, [MarshalAs(UnmanagedType.Struct)] out object pvtValue);
    [PreserveSig()]
    unsafe Foundation.HRESULT get_Name(Foundation.BSTR* Name);
    /// <summary>Gets the handle of the Windows Internet Explorer main window.</summary>
    /// <remarks>
    /// <para>Internet Explorer 7. With the introduction of tabbed browsing, the return value of this method can be ambiguous. To alleviate confusion and maintain the highest level of compatibility with existing applications, this method returns a handle to the top-level window frame, not the currently selected tab.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-iwebbrowserapp-get_hwnd#">Read more on docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    unsafe Foundation.HRESULT get_HWND(Foundation.SHANDLE_PTR* pHWND);
    [PreserveSig()]
    unsafe Foundation.HRESULT get_FullName(Foundation.BSTR* FullName);
    [PreserveSig()]
    unsafe Foundation.HRESULT get_Path(Foundation.BSTR* Path);
    [PreserveSig()]
    unsafe Foundation.HRESULT get_Visible(short* pBool);
    [PreserveSig()]
    Foundation.HRESULT put_Visible(short Value);
    [PreserveSig()]
    unsafe Foundation.HRESULT get_StatusBar(short* pBool);
    [PreserveSig()]
    Foundation.HRESULT put_StatusBar(short Value);
    [PreserveSig()]
    unsafe Foundation.HRESULT get_StatusText(Foundation.BSTR* StatusText);
    [PreserveSig()]
    Foundation.HRESULT put_StatusText(Foundation.BSTR StatusText);
    /// <summary>Sets or gets whether toolbars for the object are visible.</summary>
    /// <remarks>
    /// <para>When the IWebBrowser2::ToolBar property is set to FALSE, it is not equivalent to the "toolbar=no" feature of window.open. Instead, it turns off all user interface elements that can be considered toolbars, leaving Windows Internet Explorer in a blank state. The WebBrowser object saves the value of this property, but otherwise ignores it.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-iwebbrowserapp-get_toolbar#">Read more on docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    unsafe Foundation.HRESULT get_ToolBar(int* Value);
    /// <summary>Sets or gets whether toolbars for the object are visible.</summary>
    /// <remarks>
    /// <para>When the IWebBrowser2::ToolBar property is set to FALSE, it is not equivalent to the "toolbar=no" feature of window.open. Instead, it turns off all user interface elements that can be considered toolbars, leaving Windows Internet Explorer in a blank state. The WebBrowser object saves the value of this property, but otherwise ignores it.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//exdisp/nf-exdisp-iwebbrowserapp-put_toolbar#">Read more on docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig()]
    Foundation.HRESULT put_ToolBar(int Value);
    [PreserveSig()]
    unsafe Foundation.HRESULT get_MenuBar(short* Value);
    [PreserveSig()]
    Foundation.HRESULT put_MenuBar(short Value);
    [PreserveSig()]
    unsafe Foundation.HRESULT get_FullScreen(short* pbFullScreen);
    [PreserveSig()]
    Foundation.HRESULT put_FullScreen(short bFullScreen);
}
