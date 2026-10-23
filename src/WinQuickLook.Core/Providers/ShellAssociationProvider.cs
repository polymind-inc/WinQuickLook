using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

using WinQuickLook.Extensions;
using WinQuickLook.Interop;

namespace WinQuickLook.Providers;

public class ShellAssociationProvider
{
    public class Entry
    {
        public required string Name { get; init; }
        public required bool IsDefault { get; init; }
        public ImageSource? Icon { get; init; }
    }

    public bool TryGetDefault(FileInfo fileInfo, [NotNullWhen(true)] out Entry? entry)
    {
        entry = null;
        var pcchOut = 0u;
        var hr = PInvoke.AssocQueryString(ASSOCF.ASSOCF_INIT_IGNOREUNKNOWN, ASSOCSTR.ASSOCSTR_FRIENDLYAPPNAME, fileInfo.Extension, null, Span<char>.Empty, ref pcchOut);

        if (hr.Failed || pcchOut == 0)
        {
            return false;
        }

        Span<char> pszOut = new char[checked((int)pcchOut)];
        hr = PInvoke.AssocQueryString(ASSOCF.ASSOCF_INIT_IGNOREUNKNOWN, ASSOCSTR.ASSOCSTR_FRIENDLYAPPNAME, fileInfo.Extension, null, pszOut, ref pcchOut);

        if (hr.Value != 0 || pcchOut == 0)
        {
            return false;
        }

        entry = new Entry { Name = new string(pszOut[..(int)(pcchOut - 1)]), IsDefault = true };

        return true;
    }

    public IReadOnlyList<Entry> GetRecommends(FileInfo fileInfo)
    {
        var hr = PInvoke.SHAssocEnumHandlers(fileInfo.Extension, ASSOC_FILTER.ASSOC_FILTER_RECOMMENDED, out var enumerator);

        using var enumeratorScope = new ComScope(enumerator);

        return hr.Succeeded && enumerator is not null ? GetRecommends(enumerator) : [];
    }

    internal static IReadOnlyList<Entry> GetRecommends(IEnumAssocHandlers enumerator)
    {
        var recommends = new List<Entry>();

        foreach (var handler in EnumerateHandlers(enumerator))
        {
            var hr = handler.GetUIName(out var namePointer);
            var name = namePointer.ToStringAndFree();

            if (hr.Failed || name is null)
            {
                continue;
            }

            hr = handler.GetIconLocation(out var iconPointer, out var iconIndex);
            var iconLocation = iconPointer.ToStringAndFree();

            recommends.Add(new Entry
            {
                Name = name,
                IsDefault = false,
                Icon = hr.Succeeded ? GetIconFromLocation(iconLocation, iconIndex) : null
            });
        }

        return recommends;
    }

    public void Invoke(string appName, FileInfo fileInfo)
    {
        var hr = PInvoke.SHAssocEnumHandlers(fileInfo.Extension, ASSOC_FILTER.ASSOC_FILTER_RECOMMENDED, out var enumerator);

        using var enumeratorScope = new ComScope(enumerator);

        if (hr.Failed || enumerator is null)
        {
            return;
        }

        foreach (var handler in EnumerateHandlers(enumerator))
        {
            hr = handler.GetUIName(out var namePointer);
            var name = namePointer.ToStringAndFree();

            if (hr.Failed || appName != name)
            {
                continue;
            }

            hr = PInvoke.SHCreateItemFromParsingName(fileInfo.FullName, null, out IShellItem shellItem);

            using var itemScope = new ComScope(shellItem);

            if (hr.Failed || shellItem is null)
            {
                return;
            }

            hr = shellItem.BindToHandler<Windows.Win32.System.Com.IDataObject>(null, PInvoke.BHID_DataObject, out var dataObject);

            using var dataObjectScope = new ComScope(dataObject);

            if (hr.Succeeded && dataObject is not null)
            {
                handler.Invoke(dataObject);
            }

            return;
        }
    }

    internal static IEnumerable<IAssocHandler> EnumerateHandlers(IEnumAssocHandlers enumerator)
    {
        var handlers = new IAssocHandler[8];

        while (true)
        {
            HRESULT hr;

            try
            {
                hr = enumerator.Next(handlers, out var fetched);

                if (hr.Failed || fetched == 0)
                {
                    yield break;
                }

                for (var i = 0; i < Math.Min(fetched, (uint)handlers.Length); i++)
                {
                    if (handlers[i] is { } handler)
                    {
                        yield return handler;
                    }
                }
            }
            finally
            {
                foreach (var handler in handlers)
                {
                    using var handlerScope = new ComScope(handler);
                }

                Array.Clear(handlers);
            }

            if (hr.Value != 0)
            {
                yield break;
            }
        }
    }

    private static BitmapSource? GetIconFromLocation(string? iconLocation, int iconIndex)
    {
        if (iconLocation is null)
        {
            return null;
        }

        if (Path.IsPathFullyQualified(iconLocation))
        {
            return GetIconFromResource(iconLocation, iconIndex);
        }

        if (iconLocation.StartsWith('@'))
        {
            return GetIconFromIndirectString(iconLocation);
        }

        return null;
    }

    private static BitmapSource? GetIconFromResource(string path, int iconIndex)
    {
        Span<HICON> iconSmall = new HICON[1];

        PInvoke.ExtractIconEx(path, iconIndex, phiconSmall: iconSmall);

        try
        {
            return Imaging.CreateBitmapSourceFromHIcon(iconSmall[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                          .AsFreeze();
        }
        catch
        {
            return null;
        }
        finally
        {
            PInvoke.DestroyIcon(iconSmall[0]);
        }
    }

    private static BitmapImage? GetIconFromIndirectString(string path)
    {
        Span<char> pszOut = new char[512];

        if (PInvoke.SHLoadIndirectString(path, pszOut).Failed)
        {
            return null;
        }

        var iconImageLocation = new string(pszOut.TrimEnd('\0'));

        if (!File.Exists(iconImageLocation))
        {
            return null;
        }

        var bitmap = new BitmapImage();

        using (bitmap.Initialize())
        {
            bitmap.CreateOptions = BitmapCreateOptions.None;
            bitmap.CacheOption = BitmapCacheOption.None;
            bitmap.UriSource = new Uri(iconImageLocation);
        }

        return bitmap;
    }
}
