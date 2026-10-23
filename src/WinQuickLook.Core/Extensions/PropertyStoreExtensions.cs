using System;
using System.Linq;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace WinQuickLook.Extensions;

internal static class PropertyStoreExtensions
{
    private const ushort VariantString = 31;
    private const ushort VariantVector = 0x1000;

    public static string? GetString(this IPropertyStore propertyStore, PROPERTYKEY key)
    {
        var hr = propertyStore.GetValue(key, out var value);

        try
        {
            return hr.Succeeded && value.vt == VariantString ? Marshal.PtrToStringUni(value.pwszVal) : null;
        }
        finally
        {
            PInvoke.PropVariantClear(ref value);
        }
    }

    public static string[] GetStringArray(this IPropertyStore propertyStore, PROPERTYKEY key)
    {
        var hr = propertyStore.GetValue(key, out var value);

        try
        {
            if (hr.Failed || value.vt != (VariantVector | VariantString))
            {
                return [];
            }

            return Enumerable.Range(0, (int)value.calpwstr.cElems)
                             .Select(x => Marshal.PtrToStringUni(Marshal.ReadIntPtr(value.calpwstr.pElems, x * IntPtr.Size))!)
                             .ToArray();
        }
        finally
        {
            PInvoke.PropVariantClear(ref value);
        }
    }
}
