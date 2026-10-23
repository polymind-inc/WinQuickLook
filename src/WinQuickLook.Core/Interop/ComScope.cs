using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinQuickLook.Interop;

internal sealed class ComScope(object? value) : IDisposable
{
    private object? _value = value;

    public void Dispose()
    {
        var value = Interlocked.Exchange(ref _value, null);

        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }
}
