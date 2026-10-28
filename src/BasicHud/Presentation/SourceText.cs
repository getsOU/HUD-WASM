using System.Net;

namespace BasicHud.Presentation;

public static class SourceText
{
    // Only source game names are decoded. The result is still rendered as ordinary Razor text.
    public static string Name(string? value, string fallback = "—") => HudView.Text(value is null ? null : WebUtility.HtmlDecode(value), fallback);
}
