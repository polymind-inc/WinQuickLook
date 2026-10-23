using System.IO;

namespace WinQuickLook.Handlers;

public class GenericFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Generic;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        handlerResult = new HandlerResult { Content = new GenericPreview(fileInfo), RequestSize = new(572, 290) };
        return true;
    }
}
