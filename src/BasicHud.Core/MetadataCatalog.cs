using SOU.Live.Contracts;

namespace BasicHud.Core;

// Narrow local public metadata shapes. Camera is read as text only and is never embedded.
public sealed class BrowserMetadata
{
    public Dictionary<string, BrowserPlayer>? Players { get; set; }
    public BrowserMatch? Match { get; set; }
}
public sealed class BrowserPlayer { public string? Name { get; set; } public string? Avatar { get; set; } public string? Camera { get; set; } }
public sealed class BrowserMatch { public BrowserTeam? Left { get; set; } public BrowserTeam? Right { get; set; } }
public sealed class BrowserTeam
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? Logo { get; set; }
    public List<string>? SteamIds { get; set; }
}

public sealed record MetadataCatalog(string MatchId, IReadOnlyDictionary<string, BrowserPlayer> Players,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Rosters)
{
    public static MetadataCatalog Empty { get; } = new("", new Dictionary<string, BrowserPlayer>(), new Dictionary<string, IReadOnlyList<string>>());
    public bool Matches(ManagedMatch? match) => match is not null && MatchId == match.Id && match.TeamA.Id != match.TeamB.Id && Rosters.ContainsKey(match.TeamA.Id) && Rosters.ContainsKey(match.TeamB.Id);
    public static MetadataCatalog Bind(BrowserMetadata? data, ManagedMatch? match)
    {
        if (match is null || data?.Match?.Left is not { } left || data.Match.Right is not { } right ||
            match.TeamA.Id == match.TeamB.Id || left.Id == right.Id ||
            !(left.Id == match.TeamA.Id && right.Id == match.TeamB.Id || left.Id == match.TeamB.Id && right.Id == match.TeamA.Id)) return Empty;
        var rosters = new[] { left, right }.ToDictionary(team => team.Id!,
            team => (IReadOnlyList<string>)(team.SteamIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).Take(64).ToArray(), StringComparer.Ordinal);
        var allowed = rosters.Values.SelectMany(ids => ids).ToHashSet(StringComparer.Ordinal);
        // Ambiguous membership is omitted; it must not place one person on both teams.
        var duplicates = rosters.Values.SelectMany(ids => ids).GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var team in rosters.Keys.ToArray()) rosters[team] = rosters[team].Where(id => !duplicates.Contains(id)).ToArray();
        var players = (data.Players ?? []).Where(pair => allowed.Contains(pair.Key) && !duplicates.Contains(pair.Key) && pair.Value is not null)
            .ToDictionary(pair => pair.Key, pair => new BrowserPlayer { Name = pair.Value.Name, Avatar = pair.Value.Avatar }, StringComparer.Ordinal);
        return new(match.Id, players, rosters);
    }
}
