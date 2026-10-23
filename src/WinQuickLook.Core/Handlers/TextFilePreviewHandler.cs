using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;

using ICSharpCode.AvalonEdit;

using Windows.Win32;
using Windows.Win32.Globalization;

using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class TextFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Normal;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        using var fileStream = fileInfo.OpenReadNoLock();

        // ReSharper disable once SuspiciousTypeConversion.Global
        var multiLanguage = (IMultiLanguage2)new CMultiLanguage();

        Encoding encoding;

        try
        {
            var pnScores = 1;

            if (multiLanguage.DetectCodepageInIStream(0, 0, new ComInteropStream(fileStream), out var encodingInfo, ref pnScores).Failed || pnScores == 0)
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

        fileStream.Position = 0;

        using var reader = new StreamReader(fileStream, encoding, detectEncodingFromByteOrderMarks: true);

        var textEditor = new TextEditor();

        using (textEditor.Initialize())
        {
            textEditor.FontFamily = new FontFamily("Consolas");
            textEditor.FontSize = 14;
            textEditor.IsReadOnly = true;
            textEditor.ShowLineNumbers = true;

            textEditor.Text = reader.ReadToEnd();
            textEditor.Encoding = reader.CurrentEncoding;
            textEditor.IsModified = false;
        }

        handlerResult = new HandlerResult { Content = textEditor };

        return true;
    }
}
