using SOU.Live.Contracts;

namespace BasicHud.Presentation;

/// <summary>Retains only the last known warning when the same bomb briefly loses timing.</summary>
public sealed class BombWarningState
{
    private (Guid? Session, string? Match, string? Map, int? Round) scope;
    private (string Urgency, string Shake, double? PulseMilliseconds, bool MotionExpired, double Strength)? warning;

    public BombClockView? Update(BroadcastContext context)
    {
        var nextScope = (context.Snapshot?.SessionId, context.Match?.Id, context.Game?.Map, context.Game?.RoundNumber);
        // The map round counter can advance at round-over before the final bomb update.
        // Keep that closing round's warning until its terminal state or the next freeze/live round.
        if (warning is not null && context.Game?.RoundPhase == RoundPhase.Over
            && scope.Session == nextScope.Item1 && scope.Match == nextScope.Item2 && scope.Map == nextScope.Item3
            && nextScope.Item4 == scope.Round + 1)
            nextScope.Item4 = scope.Round;
        if (scope != nextScope || context.Game?.RoundPhase == RoundPhase.FreezeTime) warning = null;
        scope = nextScope;
        var view = BombClockView.From(context);
        if (view?.State == "planted")
        {
            if (!view.TimingKnown && warning is { } known) return view with { Urgency = known.Urgency, Shake = known.Shake, PulseMilliseconds = known.PulseMilliseconds, MotionExpired = known.MotionExpired, ShakeStrength = known.Strength };
            if (view.TimingKnown) warning = (view.Urgency, view.Shake, view.PulseMilliseconds, view.MotionExpired, view.ShakeStrength);
        }
        else if (view?.State != "defusing") warning = null;
        return view;
    }
}
