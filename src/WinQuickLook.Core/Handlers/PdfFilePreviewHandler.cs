using System.IO;

namespace WinQuickLook.Handlers;

public class PdfFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Highest;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        handlerResult = default;

        return false;
    }
}
