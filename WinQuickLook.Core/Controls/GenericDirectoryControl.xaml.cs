using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Cylinder;

namespace WinQuickLook.Controls;

public partial class GenericDirectoryControl
{
    public GenericDirectoryControl() => InitializeComponent();

    public Ref<DirectoryInfo> DirectoryInfo { get; } = new(null);

    public Ref<string> EntryCount { get; } = new("Loading...");

    private CancellationTokenSource? _cts;

    public void Open(DirectoryInfo directoryInfo)
    {
        var oldCts = _cts;
        _cts = new CancellationTokenSource();
        oldCts?.Cancel();
        oldCts?.Dispose();

        DirectoryInfo.Value = directoryInfo;
        EntryCount.Value = "Loading...";

        _ = LoadEntryCountAsync(directoryInfo, _cts.Token);
    }

    private async Task LoadEntryCountAsync(DirectoryInfo directoryInfo, CancellationToken cancellationToken)
    {
        try
        {
            var count = await Task.Run(() =>
            {
                var n = 0;
                foreach (var _ in directoryInfo.EnumerateFileSystemInfos())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    n++;
                }
                return n;
            }, cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                EntryCount.Value = $"{count} items";
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                EntryCount.Value = "Count unavailable";
            }
        }
    }
}
