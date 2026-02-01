using System;
using System.Runtime.InteropServices;

using Windows.Win32.Foundation;

// ReSharper disable once CheckNamespace
namespace Windows.Win32.UI.Shell.PropertiesSystem;

[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
public interface IPropertyStore
{
    /// <summary>This method returns a count of the number of properties that are attached to the file.</summary>
    /// <param name="cProps">A pointer to a value that indicates the property count.</param>
    /// <returns>The <code>IpropertyStore::GetCount</code> method returns a value of S_OK when the call is successful, even if the file has no properties attached. Any other code returned is an error code.</returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//propsys/nf-propsys-ipropertystore-getcount">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig]
    Foundation.HRESULT GetCount(out uint cProps);

    /// <summary>Gets a property key from the property array of an item.</summary>
    /// <param name="iProp">The index of the property key in the array of PROPERTYKEY structures. This is a zero-based index.</param>
    /// <param name="pkey">TBD</param>
    /// <returns>The <code>IPropertyStore::GetAt</code> method returns a value of S_OK if successful. Otherwise, any other code it returns must be considered to be an error code.</returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//propsys/nf-propsys-ipropertystore-getat">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig]
    Foundation.HRESULT GetAt(uint iProp, ref PROPERTYKEY pkey);

    /// <summary>This method retrieves the data for a specific property.</summary>
    /// <param name="key">TBD</param>
    /// <param name="pv">After the <c>IPropertyStore::GetValue</c> method returns successfully, this parameter points to a <a href="https://docs.microsoft.com/previous-versions/aa912007(v=msdn.10)">PROPVARIANT </a> structure that contains data about the property.</param>
    /// <returns>
    /// <para>Returns S_OK or INPLACE_S_TRUNCATED if successful, or an error value otherwise. INPLACE_S_TRUNCATED is returned to indicate that the returned PROPVARIANT was converted into a more canonical form. For example, this would be done to trim leading or trailing spaces from a string value. You must use the SUCCEEDED macro to check the return value, which treats INPLACE_S_TRUNCATED as a success code. The SUCCEEDED macro is defined in the Winerror.h file.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//propsys/nf-propsys-ipropertystore-getvalue">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig]
    Foundation.HRESULT GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);

    /// <summary>This method sets a property value or replaces or removes an existing value.</summary>
    /// <param name="key">TBD</param>
    /// <param name="propvar">TBD</param>
    /// <returns>
    /// <para>The <code>IPropertyStore::SetValue</code> method can return any one of the following: </para>
    /// <para>This doc was truncated.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//propsys/nf-propsys-ipropertystore-setvalue">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig]
    Foundation.HRESULT SetValue(ref PROPERTYKEY key, in PROPVARIANT propvar);

    /// <summary>After a change has been made, this method saves the changes.</summary>
    /// <returns>
    /// <para>The <code>IPropertyStore::Commit</code> method returns any one of the following: </para>
    /// <para>This doc was truncated.</para>
    /// </returns>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//propsys/nf-propsys-ipropertystore-commit">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    [PreserveSig]
    Foundation.HRESULT Commit();
}

// ReSharper disable once InconsistentNaming
[StructLayout(LayoutKind.Explicit)]
public struct PROPVARIANT
{
    [FieldOffset(0)]
    public ushort vt;

    [FieldOffset(2)]
    public ushort wReserved1;

    [FieldOffset(4)]
    public ushort wReserved2;

    [FieldOffset(6)]
    public ushort wReserved3;

    [FieldOffset(8)]
    public IntPtr pwszVal;

    [FieldOffset(8)]
    public CALPWSTR calpwstr;
}

// ReSharper disable once InconsistentNaming
[StructLayout(LayoutKind.Explicit)]
public struct CALPWSTR
{
    [FieldOffset(0)]
    public uint cElems;

    [FieldOffset(8)]
    public IntPtr pElems;
}
