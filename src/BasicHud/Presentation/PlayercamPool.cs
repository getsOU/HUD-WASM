using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public sealed record PlayercamSource(string SteamId, string Viewer);

public static class PlayercamPool
{
    public const int MaximumReceivers = 10;

    public static IReadOnlyList<PlayercamSource> Sources(MetadataCatalog metadata, ManagedMatch? match)
    {
        if (!metadata.Matches(match)) return [];
        return new[] { match!.TeamA.Id, match.TeamB.Id }.SelectMany(id => metadata.Rosters[id])
            .Distinct(StringComparer.Ordinal)
            .Select(id => metadata.Players.TryGetValue(id, out var profile) && PlayercamUrls.Viewer(profile.Camera) is { } viewer
                ? new PlayercamSource(id, viewer) : null)
            .OfType<PlayercamSource>().Take(MaximumReceivers).ToArray();
    }
}
