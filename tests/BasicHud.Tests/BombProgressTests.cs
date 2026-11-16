using BasicHud.Presentation;
using SOU.Live.Contracts;

internal static class BombProgressTests
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.Parse("2026-11-16T12:00:00Z");
        BroadcastContext Context(BombStatus state, double? seconds, bool? kit = null) => BroadcastContext.Empty with
        {
            Now = now,
            Snapshot = new(2, Guid.NewGuid(), 1, now, DataFreshness.Live,
                new GameState("de_mirage", MatchPhase.Live, 5, RoundPhase.Live, null, null, null,
                    [new("defuser", "Defuser", TeamSide.CounterTerrorists, PlayerActivity.Playing, 100) { DefuseKit = kit },
                     new("other", "Other", TeamSide.CounterTerrorists, PlayerActivity.Playing, 100) { DefuseKit = true }])
                { Bomb = new(state, "defuser", null, seconds), Countdown = new(CountdownPhase.Live, 90) })
        };
        BombClockView? View(BombStatus state, double? seconds, bool? kit = null) => BombClockView.From(Context(state, seconds, kit));
        check(new[] { (40d, 1000d), (30d, 775d), (20d, 550d), (10d, 325d), (5d, 212.5d), (3d, 167.5d), (0d, 100d) }
            .All(sample => BombClockView.PulseInterval(sample.Item1) == sample.Item2), "pulse cadence follows 100 + 900 * remaining / 40 throughout the countdown");
        check(new double?[] { null, double.NaN, double.PositiveInfinity }.All(seconds => BombClockView.PulseInterval(seconds) is null)
            && BombClockView.PulseInterval(10, 0) is null, "missing and invalid pulse timing stays unavailable");
        check(View(BombStatus.Planted, 10)?.PulseMilliseconds == 325
            && View(BombStatus.Defusing, 2, true)?.PulseMilliseconds is null
            && View(BombStatus.Exploded, 0)?.PulseMilliseconds is null, "formula pulses use only planted-bomb time, never defuse or terminal time");
        var pulse = Context(BombStatus.Planted, 20);
        check(View(BombStatus.Planted, 0.3) is { RemainingMilliseconds: 300, MotionExpired: false }
            && View(BombStatus.Planted, 0) is { State: "planted", MotionExpired: false, Urgency: "critical" }, "subsecond and zero bomb timing remain active and critical until the supplied terminal state");
        check(BombClockView.From(pulse with { Now = now.AddSeconds(5) })?.PulseMilliseconds == 437.5
            && BombClockView.From(pulse with { Snapshot = pulse.Snapshot! with { Freshness = DataFreshness.Stale }, Now = now.AddSeconds(5) })?.PulseMilliseconds == 550,
            "formula cadence follows synchronized elapsed time and freezes with stale data");
        check(View(BombStatus.Planting, 1.6)?.Progress == 0.5, "plant ring uses actual remaining time against the standard 3.2-second action, including mid-action entry");
        check(View(BombStatus.Defusing, 2.5, true) is { Kit: true, Icon: "kit", Progress: 0.5 }
            && View(BombStatus.Defusing, 5, false) is { Kit: false, Icon: "hand", Progress: 0.5 }, "kit and bare-hand defuses use distinct symbols and the correct five/ten-second progress scale");
        check(View(BombStatus.Defusing, 2.5) is { Kit: null, Icon: "defuse", Progress: null }, "an unknown defuser kit is not inferred from a teammate or a short remaining countdown");
        var missingActor = Context(BombStatus.Defusing, 2.5, true);
        check(BombClockView.From(missingActor with { Snapshot = missingActor.Snapshot! with { Game = missingActor.Game! with { Bomb = missingActor.Game!.Bomb! with { PlayerSteamId = "missing" } } } }) is { Kit: null, Progress: null }, "missing actor identity does not borrow the observed player's or another CT's kit");
        var halfway = Context(BombStatus.Planting, 3.2);
        check(BombClockView.From(halfway with { Now = now.AddSeconds(1.6) })?.Progress == 0.5, "action progress interpolates from SDK receive time on the existing render clock");
        check(BombClockView.From(halfway with { Now = now.AddSeconds(9) }) is { State: "planting", Progress: 1 }, "reaching zero clamps progress without inventing a planted event");
        var paused = halfway with { Snapshot = halfway.Snapshot! with { Game = halfway.Game! with { Countdown = new(CountdownPhase.Paused, 0) } }, Now = now.AddSeconds(2) };
        check(BombClockView.From(paused)?.Progress == 0
            && BombClockView.From(halfway with { Snapshot = halfway.Snapshot! with { Freshness = DataFreshness.Stale }, Now = now.AddSeconds(2) })?.Progress == 0, "paused and stale actions do not advance progress");
        check(new double?[] { null, -1, double.NaN, double.PositiveInfinity, 20 }.All(seconds => View(BombStatus.Planting, seconds)?.Progress is null), "missing, invalid or nonstandard action timing has no invented progress arc");
        check(View(BombStatus.Planted, 10.01)?.Urgency == "normal"
            && View(BombStatus.Planted, 10)?.Urgency == "warning"
            && View(BombStatus.Planted, 5.01)?.Urgency == "warning"
            && View(BombStatus.Planted, 5)?.Urgency == "critical", "bomb warning colors retain the exact ten/five-second thresholds");
        check(View(BombStatus.Planted, 10.01)?.Shake == "none"
            && View(BombStatus.Planted, 10)?.Shake == "light"
            && View(BombStatus.Planted, 5.01)?.Shake == "light"
            && View(BombStatus.Planted, 5)?.Shake == "strong"
            && View(BombStatus.Planted, 3.01)?.Shake == "strong"
            && View(BombStatus.Planted, 3)?.Shake == "strong", "the same shake stage covers the entire final five seconds without a three-second timing switch");
        check(View(BombStatus.Planted, 5)?.ShakeStrength == 0
            && View(BombStatus.Planted, 2.5)?.ShakeStrength == 0.5
            && View(BombStatus.Planted, 0)?.ShakeStrength == 1,
            "final-five-second shake strength grows continuously while the motion stage stays constant");
        check(new double?[] { null, double.NaN, double.PositiveInfinity }.All(seconds => View(BombStatus.Planted, seconds) is { Urgency: "normal", TimingKnown: false }), "missing or nonfinite bomb times remain unknown");
        check(new double[] { 0, -0.001, -0.1, -1 }.All(seconds => View(BombStatus.Planted, seconds) is { State: "planted", Urgency: "critical", TimingKnown: true }), "zero and negative bomb countdowns remain critical until a terminal state is supplied");
        var tick = Context(BombStatus.Planted, 10.5);
        check(BombClockView.From(tick with { Now = now.AddSeconds(0.5) })?.Urgency == "warning"
            && BombClockView.From(tick with { Now = now.AddSeconds(5.5) })?.Urgency == "critical", "urgency thresholds use synchronized elapsed time between SDK updates");
        check(View(BombStatus.Planted, 2)?.Progress is null
            && View(BombStatus.Defused, 0) is { Urgency: "normal", Progress: null }, "afterplant and terminal states have no action progress");

        var warning = new BombWarningState();
        var critical = Context(BombStatus.Planted, 0.1);
        var missing = critical with { Snapshot = critical.Snapshot! with { Game = critical.Game! with { Bomb = critical.Game!.Bomb! with { SecondsRemaining = null } } } };
        warning.Update(critical);
        check(warning.Update(missing) is { State: "planted", Urgency: "critical", TimingKnown: false, Shake: "strong", PulseMilliseconds: 102.25 }, "missing final bomb timing retains the last observed warning and cadence without inventing seconds or an explosion");
        warning.Update(critical with { Snapshot = critical.Snapshot! with { Game = critical.Game! with { Bomb = critical.Game!.Bomb! with { State = BombStatus.Defusing, SecondsRemaining = 2 } } } });
        check(warning.Update(missing)?.Urgency == "critical", "an aborted defuse retains the known bomb warning when afterplant timing is briefly absent");
        check(warning.Update(missing with { Snapshot = missing.Snapshot! with { Game = missing.Game! with { RoundPhase = RoundPhase.Over, RoundNumber = 6 } } })?.Urgency == "critical",
            "round-over advancing the map counter cannot clear the closing round's final bomb warning");
        var ended = missing with { Snapshot = missing.Snapshot! with { Game = missing.Game! with { Bomb = missing.Game!.Bomb! with { State = BombStatus.Exploded } } } };
        check(warning.Update(ended)?.State == "exploded" && warning.Update(missing)?.Urgency == "normal", "terminal events clear retained warnings");
        foreach (var changed in new[]
        {
            missing with { Snapshot = missing.Snapshot! with { SessionId = Guid.NewGuid() } },
            missing with { Snapshot = missing.Snapshot! with { Game = missing.Game! with { Map = "de_inferno" } } },
            missing with { Snapshot = missing.Snapshot! with { Game = missing.Game! with { RoundNumber = 6 } } },
            missing with { Snapshot = missing.Snapshot! with { Game = missing.Game! with { RoundPhase = RoundPhase.FreezeTime } } }
        })
        {
            warning = new(); warning.Update(critical);
            check(warning.Update(changed)?.Urgency == "normal", "a new session/map/round or freeze cannot inherit the previous bomb warning");
        }
        warning = new(); warning.Update(critical);
        var corrected = critical with { Snapshot = critical.Snapshot! with { Game = critical.Game! with { Bomb = critical.Game!.Bomb! with { SecondsRemaining = 20 } } } };
        check(warning.Update(corrected)?.Urgency == "normal", "a valid supplied timer correction replaces retained urgency rather than being suppressed");
        var exhausted = critical with { Snapshot = critical.Snapshot! with { Game = critical.Game! with { Bomb = critical.Game!.Bomb! with { SecondsRemaining = 0 } } } };
        warning.Update(exhausted);
        check(warning.Update(missing) is { MotionExpired: false, RemainingMilliseconds: null, TimingKnown: false }, "a missing sample after zero keeps bomb motion active without pretending the time is known");
    }
}
