using BasicHud.Core;
using SOU.Live.Contracts;

static class PortraitEffectTests
{
    public static void Run(Action<bool, string> check)
    {
        var player = new PlayerState("one", "Player", TeamSide.CounterTerrorists, null, 100);
        check(PortraitEffects.From(player, DataFreshness.Live) == PortraitEffects.None, "absent portrait-effect data never invents a status");
        check(PortraitEffects.From(player with { Smoked = 1 }, DataFreshness.Live) == new PortraitEffects(true, false, false), "smoke portrait effect follows the supplied player smoke state");
        check(PortraitEffects.From(player with { Flashed = 45 }, DataFreshness.Live) == new PortraitEffects(false, true, false), "flash portrait effect follows the supplied player flash state");
        check(PortraitEffects.From(player with { Burning = 25 }, DataFreshness.Live) == new PortraitEffects(false, false, true), "fire portrait effect follows the supplied player burning state");
        var affected = player with { Smoked = 1, Flashed = 45, Burning = 25 };
        check(PortraitEffects.From(affected, DataFreshness.Live) == new PortraitEffects(true, true, true), "simultaneous portrait effects retain all supplied conditions");
        check(PortraitEffects.From(affected with { Health = 0 }, DataFreshness.Live) == PortraitEffects.None, "known-dead portraits suppress stale condition flags");
        check(new DataFreshness?[] { null, DataFreshness.Stale, DataFreshness.Waiting, DataFreshness.Disconnected }.All(freshness => PortraitEffects.From(affected, freshness) == PortraitEffects.None), "stale, waiting or unavailable feeds never animate old portrait statuses");
        check(PortraitEffects.From(player with { Health = 80 }, DataFreshness.Live) == PortraitEffects.None, "health loss alone never invents a grenade hit effect");
        check(PortraitEffects.From(affected with { Health = null }, DataFreshness.Live).Any, "unknown health does not discard explicitly supplied live statuses as death");
        check(PortraitEffects.From(affected with { Smoked = 0, Flashed = 0, Burning = 0 }, DataFreshness.Live) == PortraitEffects.None, "cleared source conditions immediately clear the requested effect state");
    }
}
