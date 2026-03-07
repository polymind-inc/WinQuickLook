using System.IO;

using WinQuickLook.Controls;
using WinQuickLook.Extensions;

namespace WinQuickLook.Handlers;

public class GenericDirectoryPreviewHandler : DirectoryPreviewHandler
{
    public override HandlerPriorityClass PriorityClass => HandlerPriorityClass.Generic;

    protected override bool TryCreateViewer(DirectoryInfo directoryInfo, out HandlerResult? handlerResult)
    {
        var directoryControl = new GenericDirectoryControl();

        using (directoryControl.Initialize())
        {
            directoryControl.Open(directoryInfo);
        }

        handlerResult = new HandlerResult { Content = directoryControl, RequestSize = new(572, 290) };

        return true;
    }
}
