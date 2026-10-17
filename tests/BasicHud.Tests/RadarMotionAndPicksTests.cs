using BasicHud.Core;
using SOU.Live.Contracts;

static class RadarMotionAndPicksTests
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var session = Guid.NewGuid();
        var player = new PlayerState("one", "One", TeamSide.CounterTerrorists, null, 100) { Position = new(900, 900, 0), ObserverSlot = 1 };
        var missing = player with { SteamId = "two", ObserverSlot = 2 };
        var radar = new RadarState(Guid.NewGuid(), 1, 1, 10, now, [new("one", 1, new(0, 0, 0), new(1, 0, 0), 100)]);
        var game = new GameState("de_test", MatchPhase.Live, 1, RoundPhase.Live, null, null, player, [player, missing]) { Radar = radar };
        BroadcastContext Context(GameState state, DateTimeOffset time) => new(new(2, session, 1, now, DataFreshness.Live, state), null, MetadataCatalog.Empty, time, _ => null);
        var projection = new RadarProjection();
        var first = projection.Project(Context(game, now));
        check(first.Markers.Count == 1 && first.Markers[0].Key == "player:one", "fresh HLAE suppresses missing pawns rather than mixing older GSI positions");
        var moving = game with { Radar = radar with { Sequence = 2, GameTimeSeconds = 10.05, Players = [new("one", 1, new(100, 0, 0), new(1, 0, 0), 100)] }, Players = [missing, player] };
        var next = projection.Project(Context(moving, now.AddMilliseconds(50)));
        check(next.Markers.Single().MotionKey == first.Markers.Single().MotionKey, "ordinary radar movement and roster reordering preserve marker identity for linear interpolation");
        var stale = projection.Project(Context(moving, now.AddMilliseconds(751)));
        check(stale.Markers.Count == 2 && stale.Markers[0].MotionKey != next.Markers[0].MotionKey, "expired HLAE falls back as a whole frame and resets movement across source boundaries");
        var map = new ManagedMatchMap("map", "Mirage", "de_mirage", 1);
        var match = new ManagedMatch("match", "Match", 3, new("A", "Alpha", "ALP", null, 0), new("B", "Bravo", "BRA", null, 0), new(TeamSide.CounterTerrorists, TeamRecognitionStatus.Manual, "B", 0, 0), new(TeamSide.Terrorists, TeamRecognitionStatus.Manual, "A", 0, 0)) { Maps = [map], Vetos = [new("map", "Mirage", "de_mirage", MapDecisionKind.Pick, "A", 3, 1)] };
        check(MatchPresentation.MapPick(match, map) == ("PICK · ALP", "Picked by Alpha"), "series pick comes from the explicit veto team identity independently of current CT/T sides");
        check(MatchPresentation.MapPick(match with { Vetos = [match.Vetos[0] with { Type = MapDecisionKind.Decider, TeamId = null }] }, map).Label == "DECIDER", "explicit decider is not invented as a team pick");
        check(MatchPresentation.MapPick(match with { Vetos = [] }, map).Label == "Pick unknown" && MatchPresentation.MapPick(match with { Vetos = [match.Vetos[0] with { TeamId = "foreign" }] }, map).Label == "Pick unassigned", "missing pick and foreign team assignment remain unresolved");
        check(MatchPresentation.MapPick(match with { Vetos = [match.Vetos[0], match.Vetos[0] with { TeamId = "B" }] }, map).Label == "Pick unknown", "conflicting veto decisions cannot silently select a pick team");
    }
}
