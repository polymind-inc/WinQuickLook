using System.IO;
using System.Runtime.InteropServices;
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

        try
        {
            var pnScores = 1;

            if (multiLanguage.DetectCodepageInIStream(0, 0, new ComInteropStream(fileStream), out _, ref pnScores).Failed)
            {
                handlerResult = default;

                return false;
            }
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

        var textEditor = new TextEditor();

        using (textEditor.Initialize())
        {
            textEditor.FontFamily = new FontFamily("Consolas");
            textEditor.FontSize = 14;
            textEditor.IsReadOnly = true;
            textEditor.ShowLineNumbers = true;

            textEditor.Load(fileInfo.OpenReadNoLock());
        }

        handlerResult = new HandlerResult { Content = textEditor };

        return true;
    }
}
