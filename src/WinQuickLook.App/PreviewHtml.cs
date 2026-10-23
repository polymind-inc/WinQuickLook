using System;
using System.IO;
using System.Linq;
using System.Net;

using ColorCode;

namespace WinQuickLook.App;

internal static class PreviewHtml
{
    public static string Source(string source, string? languageId)
    {
        var formatter = new HtmlClassFormatter();
        var language = languageId is null ? null : Languages.FindById(languageId);
        var code = language is null ? "<pre>" + WebUtility.HtmlEncode(source) + "</pre>" : formatter.GetHtmlString(source, language);
        var count = source.Count(character => character == '\n') + 1;
        var numbers = string.Join("\n", Enumerable.Range(1, count));
        return $$"""
            <!doctype html><html><head><meta charset="utf-8">
            <style>
            :root { color-scheme: light dark; background: Canvas; color: CanvasText; }
            body { margin: 0; font: 14px/1.6 Consolas, "Cascadia Code", monospace; }
            main { display: flex; min-width: max-content; }
            pre { margin: 0 !important; font: inherit !important; }
            .numbers { position: sticky; left: 0; padding: 24px 16px; text-align: right; color: GrayText;
                       background: Canvas; border-right: 1px solid color-mix(in srgb, CanvasText 12%, Canvas);
                       user-select: none; font-variant-numeric: tabular-nums; }
            .code { padding: 24px; tab-size: 4; }
            {{formatter.GetCSSString()}}
            @media (prefers-color-scheme: dark) { .code { filter: invert(1) hue-rotate(180deg); } }
            @media (forced-colors: active) { .code { filter: none; } .numbers { border-color: CanvasText; } }
            </style></head><body><main><pre class="numbers">{{numbers}}</pre><div class="code">{{code}}</div></main></body></html>
            """;
    }

    public static string Document(string html, string? directory)
    {
        var baseUri = directory is null ? "" : new Uri(Path.TrimEndingDirectorySeparator(directory) + Path.DirectorySeparatorChar).AbsoluteUri;
        return $$"""
            <!doctype html><html><head><meta charset="utf-8"><base href="{{WebUtility.HtmlEncode(baseUri)}}">
            <style>
            :root { color-scheme: light dark; background: Canvas; color: CanvasText;
                    --stroke: color-mix(in srgb, CanvasText 12%, Canvas); }
            * { box-sizing: border-box; }
            body { font: 15px/1.7 "Segoe UI Variable Text", "Segoe UI", sans-serif;
                   max-width: 960px; margin: auto; padding: 32px; overflow-wrap: anywhere; }
            h1, h2, h3, h4 { font-weight: 600; line-height: 1.35; margin: 1.5em 0 .5em; }
            h1 { font-size: 28px; } h2 { font-size: 22px; } h3 { font-size: 18px; }
            body > :first-child { margin-top: 0; }
            img, video { max-width: 100%; height: auto; }
            a { color: LinkText; text-underline-offset: 3px; }
            code, pre { font-family: Consolas, "Cascadia Code", monospace; font-size: 13px; }
            pre { overflow: auto; padding: 16px; border: 1px solid var(--stroke); border-radius: 8px;
                  background: color-mix(in srgb, CanvasText 4%, Canvas); }
            blockquote { margin-left: 0; padding: 0 16px; border-left: 3px solid Highlight; color: GrayText; }
            hr { border: 0; border-top: 1px solid var(--stroke); margin: 24px 0; }
            table { border-collapse: collapse; width: 100%; margin: 16px 0; }
            th, td { padding: 8px 12px; border-bottom: 1px solid var(--stroke); text-align: left; }
            th { font-weight: 600; background: color-mix(in srgb, CanvasText 4%, Canvas); }
            @media (max-width: 480px) { body { padding: 20px; } }
            @media (forced-colors: active) { :root { --stroke: CanvasText; } }
            </style></head><body>{{html}}</body></html>
            """;
    }
}
