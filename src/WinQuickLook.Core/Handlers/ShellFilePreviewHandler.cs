using System.IO;

using Windows.Win32;
using Windows.Win32.UI.Shell;




namespace WinQuickLook.Handlers;

public class ShellFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.BelowNormal;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        var pcchOut = 0u;
        var riid = typeof(IPreviewHandler).GUID.ToString("B");

        if (PInvoke.AssocQueryString(ASSOCF.ASSOCF_INIT_DEFAULTTOSTAR, ASSOCSTR.ASSOCSTR_SHELLEXTENSION, fileInfo.Extension, riid, [], ref pcchOut).Failed)
        {
            handlerResult = default;

            return false;
        }

        handlerResult = new HandlerResult { Content = new ShellPreview(fileInfo) };

        return true;
    }
}
