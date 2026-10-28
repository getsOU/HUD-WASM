using SOU.Hud.Sdk;
using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public enum HudArea { Resources, Shell, Scoreboard, History, Radar, Series, Killfeed, CtRoster, TRoster, Player, Observed, Message, Sponsors, Production, Presentation }

/// <summary>Presentation dependencies only; the frame is read after a selection changes.</summary>
public sealed class HudRegionSelection(HudSessionSnapshot<HudOptions> frame, object?[] dependencies)
{
    public HudSessionSnapshot<HudOptions> Frame { get; } = frame;
    public object?[] Dependencies { get; } = dependencies;
    public static IEqualityComparer<HudRegionSelection> Comparer { get; } = new SelectionComparer();

    public static bool ResourcesChanged(HudArea area, string? playerKey, HudV3Graph? graph, BroadcastContext? previous, BroadcastContext current)
    {
        if (previous is null || !Equals(previous.ResolveMedia, current.ResolveMedia)) return true;
        // Containers pass resources to their independently subscribed descendants.
        if (area is HudArea.Shell or HudArea.CtRoster or HudArea.TRoster) return !ReferenceEquals(previous, current);
        if (area == HudArea.Radar) return !ReferenceEquals(previous.RadarCatalog, current.RadarCatalog)
            || !ReferenceEquals(previous.BoltRadarCatalog, current.BoltRadarCatalog) || !Equals(previous.ResolveAsset, current.ResolveAsset);
        if (area == HudArea.Presentation) return !ReferenceEquals(previous.Metadata, current.Metadata);
        if (area is not (HudArea.Player or HudArea.Observed)) return false;
        var steamId = (area == HudArea.Observed ? graph?.ObservedPlayer : playerKey is null ? null : graph?.Player(playerKey))?.Identity.SteamId;
        var match = graph?.ManagedMatch ?? graph?.GameMatch;
        (string?, string?) Profile(MetadataCatalog metadata) => steamId is not null && metadata.Matches(match) && metadata.Players.TryGetValue(steamId, out var profile)
            ? (profile.Name, profile.Avatar) : (null, null);
        return Profile(previous.Metadata) != Profile(current.Metadata);
    }

    public static HudRegionSelection Select(HudSessionSnapshot<HudOptions> state, HudArea area, string? playerKey = null)
    {
        var graph = state.Game;
        var info = graph?.Info;
        var panel = state.Panel;
        var values = new List<object?> { state.Ready, graph?.SessionId };
        void Match() { values.Add(graph?.ManagedMatch); values.Add(graph?.GameMatch); }
        void Field(string section, string name) => values.Add(panel?.Get(section, name)?.GetRawText());
        void Section(string section)
        {
            if (panel?.Values.TryGetValue(section, out var fields) == true)
                foreach (var field in fields.OrderBy(field => field.Key, StringComparer.Ordinal)) { values.Add(field.Key); values.Add(field.Value.GetRawText()); }
            if (panel?.References.TryGetValue(section, out var references) == true)
                foreach (var field in references.OrderBy(field => field.Key, StringComparer.Ordinal)) { values.Add(field.Key); values.Add(field.Value); }
        }
        void Player(HudPlayerPartsV3? player, bool pose = false)
        {
            values.Add(player?.Identity); values.Add(player?.Vitals); values.Add(player?.Equipment);
            values.Add(player?.Weapons); values.Add(player?.Statistics);
            if (pose) values.Add(player?.Pose);
        }
        void OrderedObjects(HudObjectKindV3 kind, HudOrderV3? order)
        {
            values.Add(order);
            if (graph is not null && order is not null && !order.Keys.IsDefault)
                foreach (var key in order.Keys) values.Add(graph.GetObject(kind, key));
        }
        switch (area)
        {
            case HudArea.Resources:
                Match();
                foreach (var team in new[] { state.Options?.Match?.Left, state.Options?.Match?.Right })
                {
                    values.Add(team?.Id);
                    if (team?.SteamIds is { IsDefault: false } ids) foreach (var id in ids) values.Add(id);
                    values.Add(null);
                }
                if (state.Options?.Players is { } profiles)
                    foreach (var profile in profiles.OrderBy(profile => profile.Key, StringComparer.Ordinal)) { values.Add(profile.Key); values.Add(profile.Value); }
                break;
            case HudArea.Shell:
                foreach (var name in new[] { "scene", "horizontalPlayercards", "hideRosters", "hideObservedPlayer", "hideRadar", "hideKillfeed", "hideRoundHistory", "hideSeries", "hideSponsors" }) Field("display", name);
                values.Add(graph?.ObservedPlayer is not null); break;
            case HudArea.Scoreboard:
                values.Add(graph?.Freshness);
                values.Add(info?.Map); values.Add(info?.Phase); values.Add(info?.RoundNumber); values.Add(info?.RoundPhase);
                values.Add(info?.Ct); values.Add(info?.T); values.Add(info?.RoundBomb); values.Add(graph?.Clock);
                values.Add(state.Options?.Caption); Match(); Field("display", "caption"); Field("display", "hideSeries"); break;
            case HudArea.History:
                values.Add(info?.RoundWins); values.Add(info?.RoundNumber); values.Add(info?.RoundPhase); values.Add(info?.Phase);
                values.Add(info?.RoundBomb); values.Add(graph?.Clock?.Countdown?.Phase); values.Add(graph?.Clock?.Bomb?.State); Match(); break;
            case HudArea.Series: values.Add(info?.Map); Match(); break;
            case HudArea.CtRoster:
            case HudArea.TRoster:
                var side = area == HudArea.CtRoster ? TeamSide.CounterTerrorists : TeamSide.Terrorists;
                values.Add(side == TeamSide.CounterTerrorists ? info?.Ct?.Name : info?.T?.Name); Match();
                if (graph?.Roster is { Players.IsDefault: false } roster)
                    foreach (var key in roster.Players)
                    {
                        var identity = graph.Player(key)?.Identity;
                        if (identity?.Side == side) { values.Add(key); values.Add(identity); }
                    }
                break;
            case HudArea.Player:
            case HudArea.Observed:
                values.Add(graph?.Freshness); values.Add(info?.Map); values.Add(info?.RoundNumber);
                values.Add(area == HudArea.Observed ? (object?)graph?.Roster?.Observed
                    : graph?.Player(playerKey ?? "")?.Identity.SteamId is { Length: > 0 } steamId && graph.ObservedPlayer?.Identity.SteamId == steamId);
                Match(); Field("display", "agentsOnly");
                Player(area == HudArea.Observed ? graph?.ObservedPlayer : playerKey is null ? null : graph?.Player(playerKey));
                if (area == HudArea.Observed) { Section("branding"); Field("display", "hideSponsors"); }
                break;
            case HudArea.Radar:
                values.Add(info?.Map); values.Add(info?.RoundNumber); values.Add(info?.RoundPhase); values.Add(info?.Phase);
                values.Add(info?.Mode);
                values.Add(graph?.Roster); values.Add(graph?.Radar); values.Add(graph?.Clock?.Bomb); values.Add(info?.RoundBomb);
                if (graph is not null)
                {
                    foreach (var player in graph.Players.OrderBy(player => player.Key, StringComparer.Ordinal))
                    {
                        values.Add(player.Key); values.Add(player.Value.Identity); values.Add(player.Value.Vitals); values.Add(player.Value.Pose);
                    }
                    if (graph.Radar is { Players.IsDefault: false } radar)
                        foreach (var key in radar.Players) values.Add(graph.GetRadarPlayer(key));
                }
                OrderedObjects(HudObjectKindV3.Grenade, graph?.GrenadeOrder); Section("radar"); break;
            case HudArea.Killfeed:
                values.Add(info?.Map); OrderedObjects(HudObjectKindV3.Kill, graph?.KillOrder); break;
            case HudArea.Message:
                values.Add(info?.RoundPhase); values.Add(info?.RoundWinner); values.Add(info?.RoundWinReason); break;
            case HudArea.Sponsors: Match(); Section("branding"); break;
            case HudArea.Production: Section("production"); break;
            case HudArea.Presentation:
                // Presentation views consume broad statistics, but not motion or provider timestamps.
                values.Add(info is null ? null : info with { Provider = null }); Match();
                values.Add(graph?.Roster); values.Add(graph?.Freshness); values.Add(graph?.Clock);
                if (graph is not null) foreach (var player in graph.Players.OrderBy(player => player.Key, StringComparer.Ordinal)) { values.Add(player.Key); Player(player.Value); }
                foreach (var section in new[] { "production", "feature", "branding", "display" }) Section(section);
                break;
        }
        return new(state, values.ToArray());
    }

    private sealed class SelectionComparer : IEqualityComparer<HudRegionSelection>
    {
        public bool Equals(HudRegionSelection? x, HudRegionSelection? y) => ReferenceEquals(x, y) || x is not null && y is not null && x.Dependencies.SequenceEqual(y.Dependencies);
        public int GetHashCode(HudRegionSelection value) { var hash = new HashCode(); foreach (var item in value.Dependencies) hash.Add(item); return hash.ToHashCode(); }
    }
}

public sealed record HudRegionPresentation(HudV3Graph? Graph, BroadcastContext Context, DisplayOptions Options)
{
    public PlayerState? Player(string key)
    {
        if (Graph?.Roster is not { Players.IsDefault: false } roster) return null;
        var index = roster.Players.IndexOf(key);
        return index >= 0 && index < Context.Players.Count ? Context.Players[index] : null;
    }
}
