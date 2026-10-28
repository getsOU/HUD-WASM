using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public sealed record PortraitEffects(bool Smoke, bool Flash, bool Fire)
{
    public static PortraitEffects None { get; } = new(false, false, false);
    public bool Any => Smoke || Flash || Fire;

    public static PortraitEffects From(PlayerState player, DataFreshness? freshness) =>
        freshness != DataFreshness.Live || player.Health is <= 0 ? None
            : new(player.Smoked is > 0, player.Flashed is > 0, player.Burning is > 0);
}
