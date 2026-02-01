using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

using WinQuickLook.Extensions;

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
        var pcchOut = 0u;

        PInvoke.AssocQueryString(ASSOCF.ASSOCF_INIT_IGNOREUNKNOWN, ASSOCSTR.ASSOCSTR_FRIENDLYAPPNAME, fileInfo.Extension, null, Span<char>.Empty, ref pcchOut);

        if (pcchOut == 0)
        {
            entry = default;

            return false;
        }

        Span<char> pszOut = stackalloc char[(int)pcchOut];

        PInvoke.AssocQueryString(ASSOCF.ASSOCF_INIT_IGNOREUNKNOWN, ASSOCSTR.ASSOCSTR_FRIENDLYAPPNAME, fileInfo.Extension, null, pszOut, ref pcchOut);

        entry = new Entry { Name = new string(pszOut), IsDefault = true };

        return true;
    }

    public IReadOnlyList<Entry> GetRecommends(FileInfo fileInfo)
    {
        if (PInvoke.SHAssocEnumHandlers(fileInfo.Extension, ASSOC_FILTER.ASSOC_FILTER_RECOMMENDED, out var enumAssocHandlers).Failed)
        {
            return [];
        }

        try
        {
            var recommends = new List<Entry>();

            var assocHandlers = new IAssocHandler[8];

            while (enumAssocHandlers.Next(assocHandlers, out var fetched).Succeeded)
            {
                if (fetched == 0)
                {
                    break;
                }

                for (var i = 0; i < fetched; i++)
                {
                    var assocHandler = assocHandlers[i];

                    try
                    {
                        assocHandler.GetUIName(out var pUiName);
                        assocHandler.GetIconLocation(out var pIconLocation, out var iconIndex);

                        var iconLocation = pIconLocation.ToString();

                        var icon = GetIconFromLocation(iconLocation, iconIndex);

                        recommends.Add(new Entry
                        {
                            Name = pUiName.ToString()!,
                            IsDefault = false,
                            Icon = icon
                        });
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(assocHandler);
                    }
                }
            }

            recommends.Sort((x, y) => Comparer<string>.Default.Compare(x.Name, y.Name));

            return recommends;
        }
        finally
        {
            Marshal.ReleaseComObject(enumAssocHandlers);
        }
    }

    public void Invoke(string appName, FileInfo fileInfo)
    {
        if (PInvoke.SHAssocEnumHandlers(fileInfo.Extension, ASSOC_FILTER.ASSOC_FILTER_RECOMMENDED, out var enumAssocHandlers).Failed)
        {
            return;
        }

        try
        {
            var assocHandlers = new IAssocHandler[8];

            while (enumAssocHandlers.Next(assocHandlers, out var fetched).Succeeded)
            {
                if (fetched == 0)
                {
                    break;
                }

                for (var i = 0; i < fetched; i++)
                {
                    var assocHandler = assocHandlers[i];

                    try
                    {
                        assocHandler.GetUIName(out var pUiName);

                        if (appName != pUiName.ToString())
                        {
                            continue;
                        }

                        PInvoke.SHCreateItemFromParsingName(fileInfo.FullName, null, out IShellItem shellItem);

                        shellItem.BindToHandler(null, PInvoke.BHID_DataObject, typeof(Windows.Win32.System.Com.IDataObject).GUID, out var dataObject);

                        assocHandler.Invoke((Windows.Win32.System.Com.IDataObject)dataObject);

                        Marshal.ReleaseComObject(dataObject);
                        Marshal.ReleaseComObject(shellItem);

                        return;
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(assocHandler);
                    }
                }
            }
        }
        finally
        {
            Marshal.ReleaseComObject(enumAssocHandlers);
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
