using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.PropertiesSystem;
using Windows.Win32.UI.WindowsAndMessaging;

using WinQuickLook.Extensions;
using WinQuickLook.Interop;

namespace WinQuickLook.Controls;

public class ShellFileControl : IDisposable
{
    public ShellFileControl(nint parent = 0)
    {
        if (parent != 0)
        {
            Handle = PInvoke.CreateWindowEx2(0, "static", "",
                WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CLIPCHILDREN,
                0, 0, 0, 0, new HWND(parent), null, null);
            if (Handle == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }
    }

    public nint Handle { get; }

    private int _width;
    private int _height;
    private bool _disposed;

    public void Resize(int x, int y, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _width = width;
        _height = height;
        if (Handle != 0)
        {
            PInvoke.SetWindowPos(new HWND(Handle), default, x, y, width, height,
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
        }
        _previewHandler?.SetRect(new RECT { right = width, bottom = height });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        try
        {
            UnloadPreviewHandler();
        }
        finally
        {
            if (Handle != 0)
            {
                PInvoke.DestroyWindow(new HWND(Handle));
            }
        }
        GC.SuppressFinalize(this);
    }

    private IPreviewHandler? _previewHandler;
    private Stream? _previewStream;

    public bool Open(FileInfo fileInfo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!TryGetPreviewHandlerCLSID(fileInfo, out var clsid))
        {
            return false;
        }

        if (!TryCreatePreviewHandler(clsid, out var previewHandler))
        {
            return false;
        }

        return Open(fileInfo, previewHandler);
    }

    internal bool Open(FileInfo fileInfo, IPreviewHandler previewHandler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stream? previewStream = null;
        var keepHandler = false;

        try
        {
            if (!TryInitializePreviewHandler(previewHandler, fileInfo, out previewStream))
            {
                return false;
            }

            var rect = new RECT { left = 0, top = 0, right = _width, bottom = _height };

            if (previewHandler.SetWindow(new HWND(Handle), rect).Failed || previewHandler.DoPreview().Failed)
            {
                return false;
            }

            UnloadPreviewHandler();

            _previewHandler = previewHandler;
            _previewStream = previewStream;

            keepHandler = true;

            return true;
        }
        finally
        {
            if (!keepHandler)
            {
                UnloadPreviewHandler(previewHandler, previewStream);
            }
        }
    }

    // ReSharper disable once InconsistentNaming
    private static bool TryGetPreviewHandlerCLSID(FileInfo fileInfo, out Guid clsid)
    {
        var pcchOut = 40u;
        Span<char> pszOut = stackalloc char[(int)pcchOut];

        var riid = typeof(IPreviewHandler).GUID.ToString("B");

        if (PInvoke.AssocQueryString(ASSOCF.ASSOCF_INIT_DEFAULTTOSTAR, ASSOCSTR.ASSOCSTR_SHELLEXTENSION, fileInfo.Extension, riid, pszOut, ref pcchOut).Failed)
        {
            clsid = Guid.Empty;

            return false;
        }

        clsid = Guid.Parse(pszOut[..(int)(pcchOut - 1)]);

        return true;
    }

    private static bool TryCreatePreviewHandler(Guid clsid, [NotNullWhen(true)] out IPreviewHandler? previewHandler)
    {
        if (PInvoke.CoCreateInstance(clsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, out previewHandler).Failed)
        {
            return false;
        }

        return previewHandler is not null;
    }

    internal static bool TryInitializePreviewHandler(IPreviewHandler previewHandler, FileInfo fileInfo, out Stream? previewStream)
    {
        previewStream = null;

        switch (previewHandler)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            case IInitializeWithFile initializeWithFile:
            {
                return initializeWithFile.Initialize(fileInfo.FullName, 0).Succeeded;
            }
            // ReSharper disable once SuspiciousTypeConversion.Global
            case IInitializeWithItem initializeWithItem:
            {
                var hr = PInvoke.SHCreateItemFromParsingName(fileInfo.FullName, null, out IShellItem shellItem);

                using var shellItemScope = new ComScope(shellItem);

                if (hr.Failed || shellItem is null)
                {
                    return false;
                }

                return initializeWithItem.Initialize(shellItem, 0).Succeeded;
            }
            // ReSharper disable once SuspiciousTypeConversion.Global
            case IInitializeWithStream initializeWithStream:
            {
                previewStream = fileInfo.OpenReadNoLock();

                return initializeWithStream.Initialize(new ComInteropStream(previewStream), 0).Succeeded;
            }
            default:
                return false;
        }
    }

    private void UnloadPreviewHandler()
    {
        var previewHandler = _previewHandler;
        var previewStream = _previewStream;

        _previewHandler = null;
        _previewStream = null;

        if (previewHandler is not null)
        {
            UnloadPreviewHandler(previewHandler, previewStream);
        }
    }

    internal static void UnloadPreviewHandler(IPreviewHandler previewHandler, Stream? previewStream)
    {
        try
        {
            using var handlerScope = new ComScope(previewHandler);

            previewHandler.Unload();
        }
        finally
        {
            previewStream?.Dispose();
        }
    }
}
