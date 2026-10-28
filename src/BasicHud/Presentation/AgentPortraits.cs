using SOU.Live.Contracts;

namespace BasicHud.Presentation;

/// <summary>Local decorative placeholders, selected by actual side and observer digit.</summary>
public static class AgentPortraits
{
    public static string? For(PlayerState player) => player.ObserverSlot is >= 0 and <= 9
        ? player.Side switch
        {
            TeamSide.CounterTerrorists => $"assets/agents/ct-{player.ObserverSlot}.png",
            TeamSide.Terrorists => $"assets/agents/t-{player.ObserverSlot}.png",
            _ => null
        }
        : null;
}
