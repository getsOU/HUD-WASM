using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using BasicHud.Core;
using SOU.Live.Contracts;

namespace BasicHud.Development;

/// <summary>Local, read-only design fixtures. This file is excluded from Release compilation.</summary>
internal sealed class PreviewFeed(string scenario) : IHudFeed
{
    public event Action<string, ConnectionState>? ConnectionChanged;

    public static PreviewFeed? TryCreate(Uri address)
    {
        if (!address.IsLoopback) return null;
        var mode = address.Query.TrimStart('?').Split('&').FirstOrDefault(value => value.StartsWith("preview=", StringComparison.Ordinal))?[8..];
        return mode is "live" or "waiting" or "stale" or "swapped" or "missing" or "bomb" ? new PreviewFeed(mode) : null;
    }

    public async IAsyncEnumerable<LiveSnapshot> WatchLiveAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ConnectionChanged?.Invoke("live", ConnectionState.Connected);
        yield return Snapshot(scenario);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    public async IAsyncEnumerable<HudPanelMessage> WatchPanelAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ConnectionChanged?.Invoke("panel", ConnectionState.Connected);
        yield return new("sou-basic-wasm", "0.1.1", new(1, []));
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private static LiveSnapshot Snapshot(string scenario)
    {
        var ct = scenario == "swapped" ? "b" : "a";
        var t = scenario == "swapped" ? "a" : "b";
        var match = new ManagedMatch("preview-match", "Synthetic fixture", 3,
            new("a", "NORTHSTAR", "NTH", null, 0), new("b", "EMBER", "EMB", null, 0),
            new(TeamSide.CounterTerrorists, TeamRecognitionStatus.Manual, ct, 0, 0),
            new(TeamSide.Terrorists, TeamRecognitionStatus.Manual, t, 0, 0));
        if (scenario == "waiting") return new(2, Guid.Parse("c6c10c64-2395-4ad0-9c3b-ef18b3f142af"), 1, null, DataFreshness.Waiting, null) { ManagedMatch = match };
        ImmutableArray<PlayerState> players = [
            Player("p1", "Aster", TeamSide.CounterTerrorists, 1, 100, "m4a1_silencer", 17, 3, 8),
            Player("p2", "Vale", TeamSide.CounterTerrorists, 2, 72, "awp", 12, 2, 10),
            Player("p3", "Orion", TeamSide.CounterTerrorists, 3, 100, "m4a1", 15, 6, 7),
            Player("p4", "Mira", TeamSide.CounterTerrorists, 4, 0, "usp_silencer", 8, 4, 12),
            Player("p5", "Nova", TeamSide.CounterTerrorists, 5, 45, "mp9", 11, 5, 10),
            Player("p6", "Flint", TeamSide.Terrorists, 6, 100, "ak47", 18, 1, 9),
            Player("p7", "Cinder", TeamSide.Terrorists, 7, 0, "glock", 10, 4, 13),
            Player("p8", "Ash", TeamSide.Terrorists, 8, 87, "ak47", 14, 5, 11),
            Player("p9", "Sable", TeamSide.Terrorists, 9, 100, "galilar", 7, 6, 12),
            Player("p0", "Rook", TeamSide.Terrorists, 0, 26, "awp", 16, 2, 8)
        ];
        var game = new GameState("de_mirage", MatchPhase.Live, 19, RoundPhase.Live, new(null, 11), new(null, 7), players[1], players)
        {
            Countdown = new(CountdownPhase.Live, 73.4),
            Bomb = scenario == "bomb" ? new(BombStatus.Planted, "p6", null, 25.7) : null
        };
        if (scenario == "missing") game = new(null, MatchPhase.Unknown, null, RoundPhase.Unknown, new(null, null), new(null, null), new(null, null, null, null, null), [new(null, null, TeamSide.CounterTerrorists, null, null)]);
        return new(2, Guid.Parse("c6c10c64-2395-4ad0-9c3b-ef18b3f142af"), 1, DateTimeOffset.UtcNow,
            scenario == "stale" ? DataFreshness.Stale : DataFreshness.Live, game)
        { ManagedMatch = match };
    }

    private static PlayerState Player(string id, string name, TeamSide side, int slot, int hp, string weapon, int kills, int assists, int deaths) =>
        new(id, name, side, PlayerActivity.Playing, hp)
        {
            ObserverSlot = slot,
            Armor = hp == 0 ? 0 : 100,
            Money = 3200 + slot * 100,
            Stats = new(kills, assists, deaths, null, null),
            Weapons = [new("weapon_0", "weapon_" + weapon, WeaponCategory.Rifle, WeaponStatus.Active, null, hp == 0 ? 0 : 23, 30, 60)]
        };
}
