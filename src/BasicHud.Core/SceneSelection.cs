namespace BasicHud.Core;

public static class SceneSelection
{
    public static IReadOnlyList<string> Names { get; } = ["live", "prematch", "lineups", "veto", "break", "postmatch", "statistics", "spotlight"];
    public static string Valid(string? value) => value is not null && Names.Contains(value, StringComparer.Ordinal) ? value : "live";
    public static string Resolve(string relativePath, string? panelScene)
    {
        var path = relativePath.Split('?', '#')[0].Trim('/');
        return path.StartsWith("views/", StringComparison.Ordinal) ? Valid(path[6..]) : Valid(panelScene);
    }
}
