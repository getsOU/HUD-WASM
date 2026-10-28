using BasicHud.Core;
using SOU.Hud.Sdk;
using SOU.Live.Contracts;

namespace BasicHud;

public static class HudClockPresentation
{
    public static HudView View(BroadcastContext context)
    {
        if (context.Snapshot is not { Game: { } game } snapshot) return HudView.Empty;
        var paused = game.Countdown?.Phase == CountdownPhase.Paused || snapshot.Freshness != DataFreshness.Live;
        double? Remaining(double? value) => HudCountdown.Remaining(value, snapshot.ReceivedAt, context.Now, paused);
        return HudView.From(snapshot with
        {
            Game = game with
            {
                Countdown = game.Countdown is { } countdown ? countdown with { SecondsRemaining = Remaining(countdown.SecondsRemaining) } : null,
                Bomb = game.Bomb is { } bomb ? bomb with { SecondsRemaining = Remaining(bomb.SecondsRemaining) } : null
            }
        });
    }
}
