using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public sealed record RoundMvpView(int RoundNumber, PlayerState Player);

// A cumulative MVP increment identifies an award, never the highest kill/damage count.
// Establish a new baseline after gaps; do not infer awards from an initial snapshot.
public sealed class RoundMvpProjection
{
    private (Guid Session, string? Map, string? Match)? binding;
    private long revision = -1;
    private int round;
    private RoundPhase? phase;
    private Dictionary<string, PlayerState> players = [];
    private Dictionary<string, PlayerState>? completedPlayers;
    private int? completedRound;
    private RoundMvpView? pending;
    private bool ambiguous;
    public RoundMvpView? View { get; private set; }

    public RoundMvpView? Update(LiveSnapshot? snapshot)
    {
        if (snapshot is not { Freshness: DataFreshness.Live, Game: { RoundNumber: > 0, Map.Length: > 0 } game }
            || game.Phase is not (MatchPhase.Live or MatchPhase.GameOver) || game.Players.IsDefaultOrEmpty)
        {
            Reset(); return null;
        }
        var nextBinding = (snapshot.SessionId, game.Map, (snapshot.ManagedMatch ?? game.ManagedMatch)?.Id);
        var nextPlayers = game.Players.Where(player => !string.IsNullOrEmpty(player.SteamId))
            .GroupBy(player => player.SteamId!, StringComparer.Ordinal).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        var nextRound = game.RoundNumber.Value;
        var reset = binding != nextBinding || nextRound < round || nextRound > round + 1;
        if (!reset && snapshot.Revision <= revision) return View;
        var decreased = nextPlayers.Any(pair => pair.Value.Stats?.Mvps is { } count
            && players.TryGetValue(pair.Key, out var previous) && previous.Stats?.Mvps is { } before && count < before);
        if (reset || decreased)
        {
            ClearAward();
        }
        else
        {
            if (game.RoundPhase == RoundPhase.Live)
            {
                // The counter and round phase can arrive in separate updates. Keep an
                // early increment pending, but never display an MVP over active play.
                if (nextRound != round || phase != RoundPhase.Live) ClearAward();
                View = null; completedPlayers = null;
                completedRound = null;
            }
            else if (game.RoundPhase == RoundPhase.Over)
            {
                // Some source updates advance the map round already at round end.
                // The completed round is the one observed in live/freeze, not +1.
                if (phase != RoundPhase.Over)
                    completedRound = phase is RoundPhase.Live or RoundPhase.FreezeTime ? round : null;
                completedPlayers = nextPlayers;
            }
            else if (game.RoundPhase == RoundPhase.FreezeTime)
            {
                if (phase == RoundPhase.Live && nextRound == round + 1)
                {
                    completedRound = round;
                    // No end-of-round snapshot: do not present partial live stats
                    // or reset freeze counters as the completed round's totals.
                    completedPlayers = null;
                }
                else if (phase == RoundPhase.FreezeTime && nextRound != round) ClearAward();
            }
            else ClearAward();

            if (View?.RoundNumber != completedRound) View = null;

            var increases = nextPlayers.Where(pair => pair.Value.Stats?.Mvps is >= 0
                && players.TryGetValue(pair.Key, out var previous) && previous.Stats?.Mvps is >= 0
                && pair.Value.Stats.Mvps > previous.Stats.Mvps).ToArray();
            if (increases.Length > 0)
            {
                var awardRound = game.RoundPhase == RoundPhase.Live && nextRound == round && phase == RoundPhase.Live
                    ? nextRound : game.RoundPhase is RoundPhase.Over or RoundPhase.FreezeTime ? completedRound : null;
                if (increases.Length != 1 || increases[0].Value.Stats!.Mvps != players[increases[0].Key].Stats!.Mvps + 1
                    || pending is { } candidate && candidate.RoundNumber == awardRound && candidate.Player.SteamId != increases[0].Key)
                {
                    pending = null; View = null; ambiguous = true;
                }
                else if (!ambiguous && awardRound is { } number)
                    pending = new(number, increases[0].Value);
            }
            if (!ambiguous && game.RoundPhase is RoundPhase.Over or RoundPhase.FreezeTime && pending is { } award
                && award.RoundNumber == completedRound && nextPlayers.ContainsKey(award.Player.SteamId!))
            {
                var awarded = completedPlayers?.GetValueOrDefault(award.Player.SteamId!)
                    ?? award.Player with { RoundKills = null, RoundDamage = null };
                View = game.RoundPhase != RoundPhase.Over || game.RoundWinner is null || game.RoundWinner == awarded.Side
                    ? award with { Player = awarded } : null;
            }
            if (View is { } visible && !nextPlayers.ContainsKey(visible.Player.SteamId!)) { pending = null; View = null; }
        }
        binding = nextBinding; revision = snapshot.Revision; round = nextRound; phase = game.RoundPhase; players = nextPlayers;
        return View;
    }

    private void Reset()
    {
        binding = null; revision = -1; round = 0; phase = null; players.Clear(); ClearAward();
    }

    private void ClearAward()
    {
        completedPlayers = null; completedRound = null; pending = null; ambiguous = false; View = null;
    }
}
