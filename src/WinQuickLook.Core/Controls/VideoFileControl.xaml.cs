using System.IO;

using Cylinder;

namespace WinQuickLook.Controls;

public partial class VideoFileControl
{
    public VideoFileControl() => InitializeComponent();

    public Ref<FileInfo> FileInfo { get; } = new(null);

    public void Open(FileInfo fileInfo)
    {
        FileInfo.Value = fileInfo;

        mediaElement.Play();
    }
}
