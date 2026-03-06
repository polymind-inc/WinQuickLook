using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Cylinder;

namespace WinQuickLook.Controls;

public partial class GenericDirectoryControl
{
    public GenericDirectoryControl() => InitializeComponent();

    public Ref<DirectoryInfo> DirectoryInfo { get; } = new(null);

    public Ref<string> EntryCount { get; } = new("Loading...");

    public void Open(DirectoryInfo directoryInfo)
    {
        DirectoryInfo.Value = directoryInfo;
        EntryCount.Value = "Loading...";

        _ = LoadEntryCountAsync(directoryInfo);
    }

    private async Task LoadEntryCountAsync(DirectoryInfo directoryInfo)
    {
        try
        {
            var count = await Task.Run(() => directoryInfo.EnumerateFileSystemInfos().Count());

            if (DirectoryInfo.Value?.FullName == directoryInfo.FullName)
            {
                EntryCount.Value = $"{count} items";
            }
        }
        catch
        {
            if (DirectoryInfo.Value?.FullName == directoryInfo.FullName)
            {
                EntryCount.Value = "Count unavailable";
            }
        }
    }
}
