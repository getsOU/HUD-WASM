using SOU.Hud.Sdk;
using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public sealed record BombClockView(string State, string Label, string Icon, bool Active, bool Paused,
    double? Progress = null, bool? Kit = null, string Urgency = "normal", bool TimingKnown = true, string Shake = "none", double? PulseMilliseconds = null,
    double? RemainingMilliseconds = null, bool MotionExpired = false, double ShakeStrength = 0)
{
    // Visual reference only: the published BombState has remaining time, not total bomb duration.
    public static double? PulseInterval(double? seconds, double referenceSeconds = 40) => seconds is { } remaining
        && double.IsFinite(remaining) && double.IsFinite(referenceSeconds) && referenceSeconds > 0
        ? 100 + 900 * Math.Clamp(remaining / referenceSeconds, 0, 1) : null;

    public static BombClockView? From(BroadcastContext context)
    {
        var game = context.Game;
        var state = game?.Bomb?.State is null or BombStatus.Unknown ? game?.RoundBomb : game.Bomb.State;
        if (state is null or BombStatus.Unknown)
            state = game?.Countdown?.Phase switch
            {
                CountdownPhase.Bomb => BombStatus.Planted,
                CountdownPhase.Defuse => BombStatus.Defusing,
                _ => state
            };
        var paused = context.Snapshot?.Freshness != DataFreshness.Live || game?.Countdown?.Phase == CountdownPhase.Paused;
        var sourceSeconds = game?.Bomb is { } bomb && bomb.State == state ? bomb.SecondsRemaining : null;
        if (sourceSeconds is null && (state == BombStatus.Planted && game?.Countdown?.Phase == CountdownPhase.Bomb
            || state == BombStatus.Defusing && game?.Countdown?.Phase == CountdownPhase.Defuse))
            sourceSeconds = game.Countdown.SecondsRemaining;
        // Bomb countdowns can cross zero before the terminal state arrives. Expired is not unknown.
        var validSeconds = sourceSeconds is { } raw && double.IsFinite(raw) && (raw >= 0 || state == BombStatus.Planted);
        var seconds = validSeconds ? HudCountdown.Remaining(Math.Max(0, sourceSeconds!.Value), context.Snapshot?.ReceivedAt, context.Now, paused) : null;
        var actorId = game?.Bomb?.PlayerSteamId;
        var actor = !string.IsNullOrWhiteSpace(actorId) ? context.Players.FirstOrDefault(player => player.SteamId == actorId)
            ?? (game?.ObservedPlayer?.SteamId == actorId ? game.ObservedPlayer : null) : null;
        var kit = state == BombStatus.Defusing ? actor?.DefuseKit : null;
        // Standard CS2 action durations, not the observed first sample (which can arrive mid-action).
        // Never use a round countdown as a plant timer or a bomb countdown as defuse progress.
        double? duration = state switch { BombStatus.Planting => 3.2, BombStatus.Defusing => kit switch { true => 5, false => 10, _ => null }, _ => null };
        var progress = duration is { } total && seconds is { } left && sourceSeconds <= total + 0.1
            ? (double?)Math.Clamp(1 - left / total, 0, 1) : null;
        var urgency = state == BombStatus.Planted && seconds is { } remaining
            ? remaining <= 5 ? "critical" : remaining <= 10 ? "warning" : "normal" : "normal";
        var shake = state == BombStatus.Planted && seconds is { } bombSeconds
            ? bombSeconds <= 5 ? "strong" : bombSeconds <= 10 ? "light" : "none" : "none";
        var strength = shake == "strong" && seconds is { } timeLeft ? Math.Clamp((5 - timeLeft) / 5, 0, 1) : 0;
        return state switch
        {
            BombStatus.Planting => new("planting", "PLANTING", "bomb", true, paused, progress, RemainingMilliseconds: seconds * 1000, MotionExpired: seconds <= 0),
            BombStatus.Planted => new("planted", "BOMB PLANTED", "bomb", true, paused, Urgency: urgency, TimingKnown: seconds is not null, Shake: shake, PulseMilliseconds: PulseInterval(seconds), RemainingMilliseconds: seconds * 1000, ShakeStrength: strength),
            BombStatus.Defusing => new("defusing", kit switch { true => "DEFUSING WITH KIT", false => "DEFUSING WITHOUT KIT", _ => "DEFUSING" },
                kit switch { true => "kit", false => "hand", _ => "defuse" }, true, paused, progress, kit, RemainingMilliseconds: seconds * 1000, MotionExpired: seconds <= 0),
            BombStatus.Defused => new("defused", "DEFUSED", "check", false, paused),
            BombStatus.Exploded => new("exploded", "EXPLODED", "explosion", false, paused),
            _ => null
        };
    }
}
