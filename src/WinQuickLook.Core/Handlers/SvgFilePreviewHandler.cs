using System.IO;

namespace WinQuickLook.Handlers;

public class SvgFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Highest;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        handlerResult = default;

        return false;
    }
}
