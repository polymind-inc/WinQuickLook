using System.IO;
using System.Windows.Media.Imaging;

using Cylinder;

using WinQuickLook.Providers;

namespace WinQuickLook.Controls;

public partial class AudioFileControl
{
    private static readonly ShellPropertyProvider s_shellPropertyProvider = new();
    private static readonly ShellThumbnailProvider s_shellThumbnailProvider = new();

    public AudioFileControl() => InitializeComponent();

    public Ref<FileInfo> FileInfo { get; } = new(null);

    public Ref<BitmapSource?> Thumbnail { get; } = new(null);

    public void Open(FileInfo fileInfo)
    {
        FileInfo.Value = fileInfo;
        Thumbnail.Value = s_shellThumbnailProvider.GetImage(fileInfo);

        var audioProperties = s_shellPropertyProvider.GetAudioProperties(fileInfo);

        title.Text = audioProperties?.Title;
        artist.Text = audioProperties?.Artist;
        album.Text = audioProperties?.Album;

        mediaElement.Play();
    }
}
