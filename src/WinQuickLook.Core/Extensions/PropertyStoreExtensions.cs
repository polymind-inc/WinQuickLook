using System;
using System.Linq;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace WinQuickLook.Extensions;

internal static class PropertyStoreExtensions
{
    public static string? GetString(this IPropertyStore propertyStore, PROPERTYKEY key)
    {
        if (propertyStore.GetValue(key, out var value).Failed)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUni(value.pwszVal);
        }
        finally
        {
            PInvoke.PropVariantClear(ref value);
        }
    }

    public static string[] GetStringArray(this IPropertyStore propertyStore, PROPERTYKEY key)
    {
        if (propertyStore.GetValue(key, out var value).Failed)
        {
            return [];
        }

        try
        {
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
