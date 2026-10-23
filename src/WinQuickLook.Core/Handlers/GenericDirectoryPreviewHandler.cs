using System.IO;

namespace WinQuickLook.Handlers;

public class GenericDirectoryPreviewHandler : DirectoryPreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Generic;

    protected override bool TryCreateViewer(DirectoryInfo directoryInfo, out HandlerResult? handlerResult)
    {
        handlerResult = new HandlerResult { Content = new GenericPreview(directoryInfo), RequestSize = new(572, 290) };
        return true;
    }
}
