using BasicHud.Presentation;
using SOU.Live.Contracts;

internal static class RoundMvpTests
{
    public static void Run(Action<bool, string> check)
    {
        var session = Guid.NewGuid();
        var player = new PlayerState("one", "Awarded player", TeamSide.CounterTerrorists, PlayerActivity.Playing, 100)
        { ObserverSlot = 1, Stats = new(12, 1, 3, 2, 0), RoundKills = 2, RoundDamage = 200 };
        LiveSnapshot Frame(long revision, RoundPhase phase, PlayerState pawn, int round = 10) =>
            new(2, session, revision, DateTimeOffset.UtcNow, DataFreshness.Live,
                new("de_mirage", MatchPhase.Live, round, phase, new("CT", 4), new("T", 6), pawn, [pawn]));
        var before = Frame(1, RoundPhase.Live, player);
        var awarded = player with { Stats = player.Stats with { Mvps = 3 } };
        var over = Frame(2, RoundPhase.Over, awarded);
        var freeze = Frame(3, RoundPhase.FreezeTime, awarded with { RoundKills = 0, RoundDamage = 0 }, 11);
        var projection = new RoundMvpProjection();
        check(projection.Update(over) is null, "joining after a round ends does not invent an MVP from cumulative totals");
        projection = new();
        check(projection.Update(before) is null && projection.Update(over) is { RoundNumber: 10, Player.RoundKills: 2, Player.RoundDamage: 200 },
            "an observed single MVP increment displays the awarded player and the completed round's source statistics");
        check(projection.Update(over) == projection.View && projection.Update(freeze) is { RoundNumber: 10, Player.RoundDamage: 200 },
            "repeat renders and next-round freeze retain the award without replacing completed statistics with reset zeros");
        check(projection.Update(Frame(4, RoundPhase.Live, freeze.Game!.Players[0], 11)) is null, "the MVP card clears when the next live round begins");
        projection = new(); projection.Update(before);
        projection.Update(Frame(2, RoundPhase.Over, player));
        check(projection.Update(freeze) is { RoundNumber: 10, Player.RoundKills: 2, Player.RoundDamage: 200 },
            "an award delivered during next-round freeze uses the cached completed round, not new-round counters");
        projection.Update(null);
        check(projection.Update(over) is null, "disconnect clears both MVP display and its counter baseline");
        projection = new(); projection.Update(before); projection.Update(over);
        check(projection.Update(over with { Revision = 3, Freshness = DataFreshness.Stale }) is null
            && projection.Update(over with { Revision = 4 }) is null, "stale data clears the award and fresh data establishes a new baseline");
        projection = new(); projection.Update(before);
        check(projection.Update(over with { SessionId = Guid.NewGuid() }) is null
            && projection.Update(over with { Revision = 3, Game = over.Game! with { Map = "de_nuke" } }) is null,
            "session and map changes cannot carry an MVP into another match");
        projection = new(); projection.Update(before);
        check(projection.Update(Frame(2, RoundPhase.Over, awarded with { Stats = awarded.Stats! with { Mvps = 4 } })) is null,
            "a multi-award jump is ambiguous and never attributed to the current round");
        projection = new(); projection.Update(before);
        var other = player with { SteamId = "two" };
        projection.Update(before with { Revision = 2, Game = before.Game! with { Players = [player, other] } });
        check(projection.Update(over with { Revision = 3, Game = over.Game! with { Players = [awarded, other with { Stats = awarded.Stats }] } }) is null,
            "simultaneous MVP increments do not arbitrarily select one player");
        projection = new(); projection.Update(before); projection.Update(over);
        check(projection.Update(Frame(3, RoundPhase.Over, player)) is null, "a decreased MVP counter clears a previous award");
        projection = new(); projection.Update(before);
        check(projection.Update(Frame(2, RoundPhase.Over, awarded, 12)) is null, "skipped rounds establish a baseline instead of inferring an award");
        projection = new(); projection.Update(before);
        check(projection.Update(Frame(2, RoundPhase.Over, awarded with { RoundKills = null, RoundDamage = null })) is { Player.RoundKills: null, Player.RoundDamage: null },
            "missing round statistics stay unknown rather than becoming zero or match totals");
        projection = new(); projection.Update(before);
        check(projection.Update(Frame(2, RoundPhase.Live, awarded)) is null
            && projection.Update(Frame(3, RoundPhase.Over, awarded)) is { RoundNumber: 10, Player.RoundKills: 2, Player.RoundDamage: 200 },
            "an MVP counter arriving before the round-over phase is retained pending and displayed when the round ends");
        projection = new(); projection.Update(before);
        check(projection.Update(Frame(2, RoundPhase.Over, awarded, 11)) is { RoundNumber: 10, Player.RoundDamage: 200 }
            && projection.Update(freeze) is { RoundNumber: 10, Player.RoundDamage: 200 },
            "a round counter advancing at round end does not erase the MVP and freeze keeps the completed round label");
        check(projection.Update(Frame(4, RoundPhase.Live, awarded, 11)) is null,
            "live play clears an award even when the source round number already advanced during round-over");
        projection = new(); projection.Update(before); projection.Update(over);
        check(projection.Update(Frame(3, RoundPhase.Over, awarded, 11)) is { RoundNumber: 10 }
            && projection.Update(freeze with { Revision = 4 }) is { RoundNumber: 10 },
            "a later round-number update during the over phase preserves the original award identity");
        projection = new(); projection.Update(before);
        check(projection.Update(freeze) is { RoundNumber: 10, Player.RoundKills: null, Player.RoundDamage: null },
            "a direct live-to-freeze transition can display the observed award without claiming partial or reset round totals");
    }
}
