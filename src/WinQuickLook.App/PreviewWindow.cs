using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;

using Windows.System;

using WinQuickLook.Handlers;
using WinQuickLook.Providers;

using static Microsoft.UI.Reactor.Factories;

namespace WinQuickLook.App;

public sealed record PreviewWindowProps(
    string PreviewKey,
    PreviewContent Content,
    ShellAssociationProvider.Entry? DefaultApp,
    ShellAssociationProvider Associations,
    Action Close,
    Action<FileInfo, ShellAssociationProvider.Entry> OpenWith);

public sealed class PreviewWindow : Component<PreviewWindowProps>
{
    public override Element Render()
    {
        var item = Props.Content.FileSystemInfo;
        var file = item as FileInfo;
        var (error, setError) = UseState<string?>(null);
        UseEffect(() => { setError(null); }, Props.PreviewKey);
        var (width, _) = UseWindowSize();
        var applications = UseMemo(() =>
        {
            try
            {
                return file is null ? Array.Empty<ShellAssociationProvider.Entry>() : Props.Associations.GetRecommends(file);
            }
            catch
            {
                return (IReadOnlyList<ShellAssociationProvider.Entry>)[];
            }
        }, Props.PreviewKey);

        void Open(ShellAssociationProvider.Entry application)
        {
            if (file is null)
            {
                return;
            }
            try
            {
                Props.OpenWith(file, application);
            }
            catch (Exception exception)
            {
                setError(exception.Message);
            }
        }

        var menuItems = applications.Select(application =>
            (MenuFlyoutItemBase)MenuItem(application.Name, () => Open(application))).ToArray();
        var flyout = MenuItems(menuItems.Length > 0
            ? menuItems
            : [MenuItem("No other apps available") with { IsEnabled = false }]);
        Element? openButton = file is null ? null : Props.DefaultApp is { } defaultApp
            ? SplitButton("Open", () => Open(defaultApp), flyout)
                .ToolTip("Open with " + defaultApp.Name)
                .AutomationName("Open with " + defaultApp.Name)
            : DropDownButton("Open with", flyout).AutomationName("Choose an application");

        var parentPath = item is DirectoryInfo directory ? directory.Parent?.FullName : file?.DirectoryName;
        var titleBar = TitleBar(item.Name)
            .Icon(FontIcon(item is DirectoryInfo ? "\uE8B7" : "\uE8A5"))
            .Subtitle(width >= 640 ? AppParameters.Title : "");

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Auto, GridSize.Star(), GridSize.Auto],
            titleBar.Grid(row: 0),
            Border(
                Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
                    Component<PreviewBody, PreviewContent>(Props.Content).WithKey(Props.PreviewKey),
                    error is null ? null : InfoBar("Could not open application", error)
                        .Severity(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error)
                        .Margin(16).VAlign(VerticalAlignment.Top)
                )
            ).CornerRadius(8).Background(Theme.LayerFill).WithBorder(Theme.CardStroke)
                .Margin(left: 12, top: 4, right: 12).Grid(row: 1),
            Grid(columns: [GridSize.Star(), GridSize.Auto], rows: [GridSize.Auto],
                VStack(2,
                    HStack(8,
                        Caption(Describe(Props.Content)).Foreground(Theme.SecondaryText),
                        Caption("·").Foreground(Theme.SecondaryText),
                        Component<FileDetails, FileSystemInfo>(item)
                    ),
                    Caption(parentPath ?? item.FullName)
                        .TextTrimming(TextTrimming.CharacterEllipsis)
                        .Foreground(Theme.SecondaryText)
                        .ToolTip(item.FullName)
                ).VAlign(VerticalAlignment.Center).Margin(right: 16).Grid(column: 0),
                openButton?.VAlign(VerticalAlignment.Center).Grid(column: 1)
            ).Padding(16, 12).Grid(row: 2)
        ).OnKeyDown((_, e) =>
        {
            if (e.Key == VirtualKey.Escape)
            {
                Props.Close();
                e.Handled = true;
            }
        });
    }

    private static string Describe(PreviewContent content) => content switch
    {
        ImagePreview => "Image",
        MediaPreview { IsVideo: true } => "Video",
        MediaPreview => "Audio",
        HtmlPreview { Html: not null } => "Markdown",
        HtmlPreview => "HTML",
        TextPreview { Language: not null } => "Source code",
        TextPreview => "Text",
        _ when content.FileSystemInfo is DirectoryInfo => "Folder",
        _ => "File"
    };
}
