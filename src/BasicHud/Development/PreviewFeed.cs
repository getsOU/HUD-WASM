using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BasicHud.Core;
using SOU.Live.Contracts;

namespace BasicHud.Development;

/// <summary>Local, read-only design fixtures. This file is excluded from Release compilation.</summary>
internal sealed class PreviewFeed(string scenario) : IHudFeed
{
    private static readonly DateTimeOffset PlannedStart = DateTimeOffset.UtcNow.AddMinutes(5);
    public event Action<string, ConnectionState>? ConnectionChanged;

    public static PreviewFeed? TryCreate(Uri address)
    {
        if (!address.IsLoopback) return null;
        var mode = address.Query.TrimStart('?').Split('&').FirstOrDefault(value => value.StartsWith("preview=", StringComparison.Ordinal))?[8..];
        return mode is "live" or "prematch" or "lineups" or "veto" or "break" or "postmatch" or "statistics" or "spotlight" or "waiting" or "stale" or "swapped" or "missing" or "bomb" or "overview" or "production" or "avatar-effects" ? new PreviewFeed(mode) : null;
    }

    public async IAsyncEnumerable<LiveSnapshot> WatchLiveAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ConnectionChanged?.Invoke("live", ConnectionState.Connected);
        var revision = 0L;
        var previewClock = System.Diagnostics.Stopwatch.StartNew();
        while (!cancellationToken.IsCancellationRequested)
        {
            var snapshot = Snapshot(scenario);
            if (scenario == "avatar-effects" && snapshot.Game is { } game)
            {
                // Debug-only source pulses exercise full cycles and reversals halfway through exit.
                var phase = previewClock.Elapsed.TotalSeconds % 8;
                var active = phase < 2 || phase is >= 2.25 and < 2.5 || phase is >= 4 and < 5.5;
                var players = game.Players.Select(player => player with
                {
                    Smoked = active && player.ObserverSlot == 8 ? 1 : 0,
                    Flashed = active && player.ObserverSlot == 3 ? 45 : 0,
                    Burning = active && player.ObserverSlot == 0 ? 25 : 0
                }).ToImmutableArray();
                snapshot = snapshot with { Game = game with { Players = players } };
            }
            yield return snapshot with { Revision = ++revision };
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }

    public async IAsyncEnumerable<HudPanelMessage> WatchPanelAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ConnectionChanged?.Invoke("panel", ConnectionState.Connected);
        JsonElement Text(string value) => JsonSerializer.SerializeToElement(value);
        var values = new Dictionary<string, Dictionary<string, JsonElement>>
        {
            ["display"] = new() { ["scene"] = Text(SceneSelection.Valid(scenario)) },
            ["production"] = new() { ["ticker"] = Text("Synthetic local preview · no game input"), ["lowerTitle"] = Text("Example analyst"), ["lowerSubtitle"] = Text("Operator-controlled lower third"), ["replayLabel"] = Text("Preview") },
            ["feature"] = new() { ["player"] = Text("p2"), ["team"] = Text("a"), ["match"] = Text("preview-match"), ["headline"] = Text("Player spotlight"), ["subline"] = Text("Explicit production selection") },
            ["branding"] = new() { ["logo"] = Text("/cast-media/preview.svg"), ["sponsors"] = JsonSerializer.SerializeToElement(new[] { "/cast-media/preview.svg" }) }
        };
        if (scenario == "overview") values["radar"] = new() { ["overview"] = Text("/cast-media/preview.svg"), ["mapName"] = Text("de_mirage"), ["minX"] = Text("0"), ["maxX"] = Text("2500"), ["minY"] = Text("0"), ["maxY"] = Text("2500") };
        yield return new("sou-basic-wasm", "0.3.0", new(1, values)
        {
            References = new()
            {
                ["feature"] = new()
                {
                    ["player"] = new("p2", "Vale", "/cast-media/preview.svg", "a", false),
                    ["team"] = new("a", "NORTHSTAR", "/cast-media/preview.svg", "a", false),
                    ["match"] = new("preview-match", "Synthetic fixture", null, null, false)
                }
            }
        });
        if (scenario == "production") yield return new("sou-basic-wasm", "0.3.0", Action: new(Guid.Parse("9829d5b3-37c3-4f68-a489-bf6dece1cd31"), "production", "lowerThird", "show"));
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    public static BrowserMetadata Metadata(bool includePortraits = true) => new()
    {
        Match = new() { Left = new() { Id = "a", SteamIds = ["p1", "p2", "p3", "p4", "p5"] }, Right = new() { Id = "b", SteamIds = ["p6", "p7", "p8", "p9", "p0"] } },
        Players = new[] { ("p1", "Aster"), ("p2", "Vale"), ("p3", "Orion"), ("p4", "Mira"), ("p5", "Nova"), ("p6", "Flint"), ("p7", "Cinder"), ("p8", "Ash"), ("p9", "Sable"), ("p0", "Rook") }
            .ToDictionary(item => item.Item1, item => new BrowserPlayer { Name = item.Item2, Avatar = includePortraits ? "/cast-media/preview.svg" : null })
    };

    private static LiveSnapshot Snapshot(string scenario)
    {
        var ct = scenario == "swapped" ? "b" : "a";
        var t = scenario == "swapped" ? "a" : "b";
        var match = new ManagedMatch("preview-match", "Synthetic fixture", 3,
            new("a", "NORTHSTAR", "NTH", "/cast-media/preview.svg", scenario == "postmatch" ? 2 : 1), new("b", "EMBER", "EMB", "/cast-media/preview.svg", 0),
            new(TeamSide.CounterTerrorists, TeamRecognitionStatus.Manual, ct, 0, 0),
            new(TeamSide.Terrorists, TeamRecognitionStatus.Manual, t, 0, 0))
        {
            StartsAt = PlannedStart,
            Maps = [new("m1", "Mirage", "de_mirage", 1) { Status = MapResultStatus.Completed, ScoreA = 13, ScoreB = 8, WinnerTeamId = "a" },
                new("m2", "Nuke", "de_nuke", 2) { Status = scenario == "postmatch" ? MapResultStatus.Completed : MapResultStatus.Live, ScoreA = scenario == "postmatch" ? 13 : 11, ScoreB = 7, WinnerTeamId = scenario == "postmatch" ? "a" : null },
                new("m3", "Dust II", "de_dust2", 3) { Status = MapResultStatus.Pending }],
            Vetos = [new("b1", "Inferno", "de_inferno", MapDecisionKind.Ban, "a", 1, null), new("b2", "Ancient", "de_ancient", MapDecisionKind.Ban, "b", 2, null),
                new("m1", "Mirage", "de_mirage", MapDecisionKind.Pick, "a", 3, 1), new("m2", "Nuke", "de_nuke", MapDecisionKind.Pick, "b", 4, 2), new("m3", "Dust II", "de_dust2", MapDecisionKind.Decider, null, 5, 3)],
            Competition = new("c1", "Synthetic Championship", "s1", "Season 1", [new("division", "Premier", CompetitionSectionKind.Division)], "Final") { Logo = "/cast-media/preview.svg", Sponsors = [new("sponsor", "Synthetic sponsor", "/cast-media/preview.svg")] }
        };
        if (scenario is "waiting" or "prematch" or "lineups") return new(2, Guid.Parse("c6c10c64-2395-4ad0-9c3b-ef18b3f142af"), 1, null, DataFreshness.Waiting, null) { ManagedMatch = match };
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
            AvailableSections = [GsiSection.Map, GsiSection.AllPlayers, GsiSection.Player, GsiSection.Round, GsiSection.Bomb, GsiSection.Grenades, GsiSection.PhaseCountdowns],
            Countdown = new(CountdownPhase.Live, 73.4),
            Bomb = new(scenario == "bomb" ? BombStatus.Planted : BombStatus.Carried, "p6", MiragePosition(6), scenario == "bomb" ? 25.7 : null),
            Radar = new(Guid.Parse("84bb12b2-7165-49c7-909e-1cc877d587b4"), 1, 1, 125, DateTimeOffset.UtcNow, players.Select(player => new RadarPlayer(player.SteamId!, 1, player.Position!, player.Forward!, player.Health!.Value)).ToImmutableArray()),
            Grenades = [new("smoke1", GrenadeKind.Smoke, "p3", MiragePosition(8), null, null, 5)],
            RoundWins = Enumerable.Range(1, 18).Select(round => new RoundResult(round, round % 3 == 0 ? TeamSide.Terrorists : TeamSide.CounterTerrorists, round % 4 == 0 ? RoundWinReason.BombDefused : RoundWinReason.Elimination)).ToImmutableArray(),
            KillFeed = [new(1, Guid.Parse("84bb12b2-7165-49c7-909e-1cc877d587b4"), 1, 124, new(1, "p1", "Aster", TeamSide.CounterTerrorists), new(2, "p7", "Cinder", TeamSide.Terrorists), new(3, "p2", "Vale", TeamSide.CounterTerrorists), "m4a1", WeaponKind.Unknown, true, true, false, false, false, false)]
        };
        if (scenario == "postmatch") game = game with { Phase = MatchPhase.GameOver, RoundPhase = RoundPhase.Over, RoundWinner = TeamSide.CounterTerrorists, RoundWinReason = RoundWinReason.Elimination };
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
            EquipmentValue = 4100,
            Helmet = true,
            DefuseKit = side == TeamSide.CounterTerrorists,
            RoundKills = slot % 3,
            RoundDamage = slot * 27,
            Flashed = slot == 3 ? 45 : 0,
            Smoked = slot == 8 ? 1 : 0,
            Burning = slot == 0 ? 25 : 0,
            Position = MiragePosition(slot),
            Forward = new(Math.Cos(slot), Math.Sin(slot), 0),
            Stats = new(kills, assists, deaths, slot % 3, kills * 2 + assists),
            RecordedStats = new(12, 1200 + slot * 30, kills, kills / 2, false),
            Weapons = [new("weapon_0", "weapon_" + weapon, WeaponCategory.Rifle, WeaponStatus.Active, null, hp == 0 ? 0 : 23, 30, 60),
                new("weapon_1", "weapon_knife", WeaponCategory.Knife, WeaponStatus.Holstered, null, null, null, null), new("weapon_2", "weapon_flashbang", WeaponCategory.Grenade, WeaponStatus.Holstered, null, null, null, null)]
        };

    private static Vector3State MiragePosition(int slot)
    {
        // Walkable points in the original Mirage overview: B, window, mid,
        // market, CT approach, A and T approaches. All fixtures use this map.
        var (x, y) = slot switch
        {
            1 => (24.0, 28.0),
            2 => (40.5, 43.0),
            3 => (49.0, 42.0),
            4 => (20.0, 44.0),
            5 => (33.0, 68.0),
            6 => (55.0, 77.0),
            7 => (60.0, 63.0),
            8 => (55.0, 46.0),
            9 => (86.0, 55.0),
            _ => (70.0, 42.0)
        };
        const double worldSize = 5.02 * 1024;
        return new(x / 100 * worldSize - 3240, (1 - y / 100) * worldSize - 3410, -120);
    }
}
