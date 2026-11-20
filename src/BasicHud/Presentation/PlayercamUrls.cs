namespace BasicHud.Presentation;

public static class PlayercamUrls
{
    public static string? Viewer(string? url) => url is not null
        && System.Text.RegularExpressions.Regex.IsMatch(url, @"\A/playercams/local/view/[a-fA-F0-9]{32}#[a-fA-F0-9]{32}:[a-fA-F0-9]{64}\z") ? url
        : Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == "https" && uri.IsDefaultPort && (uri.Host is "getsou.cam" or "www.getsou.app") && uri.UserInfo.Length == 0
        && uri.AbsolutePath == "/playercams/view" && uri.Query.Length == 0
        && System.Text.RegularExpressions.Regex.IsMatch(uri.Fragment, @"\A#[a-fA-F0-9]{32}:[a-fA-F0-9]{64}\z") ? url : null;
}
