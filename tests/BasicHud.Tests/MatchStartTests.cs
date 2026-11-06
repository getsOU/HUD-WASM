using BasicHud.Presentation;
using SOU.Hud.Sdk;
using SOU.Live.Contracts;

static class MatchStartTests
{
    public static void Run(Action<bool, string> check)
    {
        var planned = DateTimeOffset.Parse("2026-11-06T19:00:00+01:00", System.Globalization.CultureInfo.InvariantCulture);
        var delayed = planned.AddMinutes(45);
        var regular = new MatchStartView(planned, null);
        var postponed = new MatchStartView(planned, delayed);
        check(regular.Effective == planned && !regular.IsDelayed && regular.Countdown(planned.AddMinutes(-5)) == "5:00", "regular match start is used when no delay is supplied");
        check(postponed.Effective == delayed && postponed.IsDelayed && postponed.Countdown(planned.AddMinutes(10)) == "35:00", "delayed start takes precedence even after the regular start has passed");
        check(new MatchStartView(null, delayed).Countdown(planned) == "45:00", "an explicit delayed start works without a regular start");
        check((postponed with { Delayed = delayed.AddMinutes(15) }).Countdown(planned.AddMinutes(10)) == "50:00", "changing the delayed time immediately changes the countdown target");
        check((postponed with { Delayed = null }).Countdown(planned.AddMinutes(10)) == "", "clearing a delay restores the actual regular start without keeping the old delay");
        check(postponed.Countdown(delayed) == "" && postponed.Countdown(delayed.AddMinutes(1)) == "", "an elapsed delayed start never counts below zero or claims the match is live");
        check(new MatchStartView(null, null).Countdown(planned) == "Start time not set", "no supplied start time remains unknown");
        foreach (var timestamp in new[] { delayed, delayed.AddMonths(6) })
        {
            var local = TimeZoneInfo.ConvertTime(timestamp, TimeZoneInfo.Local).ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            check(MatchStartView.Timestamp(timestamp) == local && MatchStartView.Timestamp(timestamp.ToOffset(TimeSpan.FromHours(-5))) == local, "start timestamps use local time without a timezone suffix, independent of source offset in winter and summer");
        }
        check(postponed.Countdown(planned.ToOffset(TimeSpan.FromHours(-5))) == "45:00", "local timestamp formatting leaves the absolute countdown target unchanged");
        check(postponed.HasElapsed(delayed) && !postponed.HasElapsed(delayed.AddTicks(-1)) && !postponed.HasElapsed(planned.AddMinutes(10)), "visibility follows the effective target exactly and preserves an active delay after the original time passes");
        var match = new ManagedMatch("start", "Start", 1, new("a", "Alpha", "A", null, 0), new("b", "Bravo", "B", null, 0),
            new(TeamSide.CounterTerrorists, TeamRecognitionStatus.Manual, "a", 0, 0), new(TeamSide.Terrorists, TeamRecognitionStatus.Manual, "b", 0, 0))
        { StartsAt = planned, DelayedStartsAt = delayed };
        var at = planned.AddMinutes(10);
        var snapshot = new LiveSnapshot(2, Guid.NewGuid(), 1, at, DataFreshness.Waiting, null) { ManagedMatch = match };
        var context = BroadcastContext.Empty with { Snapshot = snapshot, Now = at };
        check(context.MatchStart.Effective == match.EffectiveStartsAt && context.StartCountdown == "35:00"
            && HudCountdown.MatchStartRemaining(match, at) == 2100, "managed delayed start reaches the countdown before any GSI and agrees with the SDK time helper");
        var game = new GameState(null, MatchPhase.Unknown, null, RoundPhase.Unknown, new(null, null), new(null, null), null, []) { ManagedMatch = match };
        var fallback = context with { Snapshot = snapshot with { ManagedMatch = null, Game = game } };
        check(fallback.MatchStart.IsDelayed && fallback.StartCountdown == "35:00", "compatible game-match path retains the same delayed-start semantics");
        var cleared = context with { Snapshot = snapshot with { ManagedMatch = match with { DelayedStartsAt = null } } };
        check(!cleared.MatchStart.IsDelayed && cleared.StartCountdown == "", "clearing the actual SDK match delay restores the regular schedule");
    }
}
