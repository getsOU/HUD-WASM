using SOU.Live.Contracts;

namespace BasicHud.Presentation;

/// <summary>Local decorative placeholders, selected by actual side and observer digit.</summary>
public static class AgentPortraits
{
    // Recorded players have no observer digit. Reuse the live identity when present;
    // otherwise choose a stable decorative portrait, never a statistical team assignment.
    public static string? ForRecorded(BroadcastContext context, string? steamId, TeamSide? recordedSide = null)
    {
        var live = context.Players.FirstOrDefault(p => steamId is not null && p.SteamId == steamId);
        var side = recordedSide ?? live?.Side;
        if (side is not (TeamSide.CounterTerrorists or TeamSide.Terrorists)) return null;
        var slot = live?.ObserverSlot is >= 0 and <= 9 ? live.ObserverSlot.Value
            : (int)((steamId ?? "").Aggregate(0u, (hash, c) => unchecked(hash * 31 + c)) % 10);
        return For(new PlayerState(steamId, null, side, null, null) { ObserverSlot = slot });
    }

    public static string? For(PlayerState player) => player.ObserverSlot is >= 0 and <= 9
        ? player.Side switch
        {
            TeamSide.CounterTerrorists => $"assets/agents/ct-{player.ObserverSlot}.png",
            TeamSide.Terrorists => $"assets/agents/t-{player.ObserverSlot}.png",
            _ => null
        }
        : null;
}
