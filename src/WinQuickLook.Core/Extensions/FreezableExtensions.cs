using System.Windows;

namespace WinQuickLook.Extensions;

internal static class FreezableExtensions
{
    public static T AsFreeze<T>(this T obj) where T : Freezable
    {
        if (obj.CanFreeze)
        {
            obj.Freeze();
        }

        return obj;
    }
}
