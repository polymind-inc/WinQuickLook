using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

using Windows.Foundation;

using Windows.Win32;
using Windows.Win32.Media.MediaFoundation;


using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class MediaFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Normal;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        if (PInvoke.MFCreateSourceReaderFromURL(fileInfo.FullName, null, out var sourceReader).Failed)
        {
            handlerResult = default;

            return false;
        }

        try
        {
            if (sourceReader.GetCurrentMediaType(PInvoke.MF_SOURCE_READER_FIRST_VIDEO_STREAM, out var videoMediaType).Succeeded)
            {
                try
                {
                    return TryCreateVideoViewer(fileInfo, videoMediaType, out handlerResult);
                }
                finally
                {
                    Marshal.ReleaseComObject(videoMediaType);
                }
            }

            if (sourceReader.GetCurrentMediaType(PInvoke.MF_SOURCE_READER_FIRST_AUDIO_STREAM, out var audioMediaType).Succeeded)
            {
                try
                {
                    return TryCreateAudioViewer(fileInfo, audioMediaType, out handlerResult);
                }
                finally
                {
                    Marshal.ReleaseComObject(audioMediaType);
                }
            }
        }
        finally
        {
            Marshal.ReleaseComObject(sourceReader);
        }

        handlerResult = default;

        return false;
    }

    private static bool TryCreateVideoViewer(FileInfo fileInfo, IMFAttributes videoMediaType, [NotNullWhen(true)] out HandlerResult? handlerResult)
    {
        if (!TryGetVideoSize(videoMediaType, out var videoSize))
        {
            handlerResult = null;

            return false;
        }

        handlerResult = new HandlerResult { Content = new MediaPreview(fileInfo, true, videoSize), RequestSize = videoSize };

        return true;
    }

    private static bool TryCreateAudioViewer(FileInfo fileInfo, IMFAttributes audioMediaType, [NotNullWhen(true)] out HandlerResult? handlerResult)
    {
        if (!TryGetAudioChannels(audioMediaType))
        {
            handlerResult = null;

            return false;
        }

        handlerResult = new HandlerResult { Content = new MediaPreview(fileInfo, false), RequestSize = new Size(700, 400) };

        return true;
    }

    internal static bool TryGetVideoSize(IMFAttributes videoMediaType, out Size size)
    {
        if (PInvoke.MFGetAttributeSize(videoMediaType, PInvoke.MF_MT_FRAME_SIZE, out var width, out var height).Failed || width == 0 || height == 0)
        {
            size = default;

            return false;
        }

        size = videoMediaType.GetUINT32(PInvoke.MF_MT_VIDEO_ROTATION, out var rotation).Succeeded && rotation is 90 or 270
            ? new Size(height, width)
            : new Size(width, height);

        return true;
    }

    private static bool TryGetAudioChannels(IMFAttributes audioMediaType)
    {
        return !audioMediaType.GetUINT32(PInvoke.MF_MT_AUDIO_NUM_CHANNELS, out var channels).Failed && channels != 0;
    }
}
