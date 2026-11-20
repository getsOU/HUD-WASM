using SOU.Live.Contracts;

namespace BasicHud.Presentation;

/// <summary>Local decorative placeholders, selected by actual side and observer digit.</summary>
public static class AgentPortraits
{
    // Camera rosters may precede GSI. Select artwork only, without assigning a
    // side or observer digit to the player or replacing a configured photo.
    public static string ForRoster(PlayerState player)
    {
        if (For(player) is { } regular) return regular;
        var hash = (player.SteamId ?? "").Aggregate(0u, (value, c) => unchecked(value * 31 + c));
        var prefix = player.Side switch
        {
            TeamSide.CounterTerrorists => "ct",
            TeamSide.Terrorists => "t",
            _ => hash % 20 < 10 ? "ct" : "t"
        };
        return $"assets/agents/{prefix}-{hash % 10}.png";
    }

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
