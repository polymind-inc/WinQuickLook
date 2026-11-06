using System.IO;
using System.Runtime.InteropServices;
using System.Text;




using Windows.Win32;
using Windows.Win32.Globalization;
using Windows.Win32.System.Com;

using WinQuickLook.Extensions;
using WinQuickLook.Interop;

namespace WinQuickLook.Handlers;

public class TextFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Normal;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        var multiLanguage = (IMultiLanguage2)new CMultiLanguage();

        Encoding encoding;

        try
        {
            var hr = PInvoke.SHCreateStreamOnFileEx(fileInfo.FullName, (uint)(STGM.STGM_READ | STGM.STGM_SHARE_DENY_NONE), 0, false, null, out var nativeStream);

            using var streamScope = new ComScope(nativeStream);

            if (hr.Failed || nativeStream is null)
            {
                handlerResult = default;

                return false;
            }

            var pnScores = 1;

            if (multiLanguage.DetectCodepageInIStream(0, 0, nativeStream, out var encodingInfo, ref pnScores).Failed || pnScores == 0)
            {
                handlerResult = default;

                return false;
            }

            var codePage = (int)encodingInfo.nCodePage;

            encoding = CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.GetEncoding(codePage);
        }
        catch
        {
            handlerResult = default;

            return false;
        }
        finally
        {
            Marshal.ReleaseComObject(multiLanguage);
        }

        using var fileStream = fileInfo.OpenReadNoLock();
        using var reader = new StreamReader(fileStream, encoding, detectEncodingFromByteOrderMarks: true);

        var text = reader.ReadToEnd();
        handlerResult = new HandlerResult { Content = new TextPreview(fileInfo, text, reader.CurrentEncoding) };

        return true;
    }
}
