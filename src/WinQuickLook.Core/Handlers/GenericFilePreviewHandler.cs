using System.IO;

using WinQuickLook.Controls;
using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class GenericFilePreviewHandler : FilePreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Generic;

    protected override bool TryCreateViewer(FileInfo fileInfo, out HandlerResult? handlerResult)
    {
        var fileControl = new GenericFileControl();

        using (fileControl.Initialize())
        {
            fileControl.Open(fileInfo);
        }

        handlerResult = new HandlerResult { Content = fileControl, RequestSize = new(572, 290) };

        return true;
    }
}
