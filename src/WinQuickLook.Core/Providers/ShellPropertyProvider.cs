using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.UI.Shell.PropertiesSystem;

using WinQuickLook.Extensions;

namespace WinQuickLook.Providers;

public class ShellPropertyProvider
{
    public class AudioProperties
    {
        public string? Title { get; init; }
        public string? Artist { get; init; }
        public string? Album { get; init; }
    }

    public AudioProperties? GetAudioProperties(FileInfo fileInfo)
    {
        if (PInvoke.SHGetPropertyStoreFromParsingName(fileInfo.FullName, null, GETPROPERTYSTOREFLAGS.GPS_DEFAULT, out IPropertyStore propertyStore).Failed)
        {
            return null;
        }

        try
        {
            return new AudioProperties
            {
                Title = propertyStore.GetString(PInvoke.PKEY_Title),
                Artist = propertyStore.GetStringArray(PInvoke.PKEY_Music_Artist).FirstOrDefault(),
                Album = propertyStore.GetString(PInvoke.PKEY_Music_AlbumTitle)
            };
        }
        finally
        {
            Marshal.ReleaseComObject(propertyStore);
        }

    }
}
