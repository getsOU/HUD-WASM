using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public static class PlayerPresentation
{
    public static IReadOnlyList<PlayerState> Ordered(IEnumerable<PlayerState> players) => players.OrderBy(player => player.ObserverSlot is >= 1 and <= 9 ? player.ObserverSlot : player.ObserverSlot == 0 ? 10 : int.MaxValue).ThenBy(player => player.SteamId, StringComparer.Ordinal).ToArray();
    public static string SideClass(TeamSide? side) => side == TeamSide.CounterTerrorists ? "ct" : side == TeamSide.Terrorists ? "t" : "neutral";
    public static string Kda(PlayerState player) => $"{BroadcastContext.Number(player.Stats?.Kills)} / {BroadcastContext.Number(player.Stats?.Deaths)} / {BroadcastContext.Number(player.Stats?.Assists)}";
    // A dead pawn may retain its previous inventory in source snapshots. It is not current equipment.
    public static IReadOnlyList<WeaponState> Weapons(PlayerState player) => player.Health is not <= 0 && player.Weapons is { IsDefault: false } weapons ? weapons : [];
    public static string Conditions(PlayerState player) => string.Join(" · ", new[] {
        player.Flashed is > 0 ? $"FLASH {player.Flashed}" : null,
        player.Smoked is > 0 ? $"SMOKE {player.Smoked}" : null,
        player.Burning is > 0 ? $"BURN {player.Burning}" : null }.Where(value => value is not null));
}
