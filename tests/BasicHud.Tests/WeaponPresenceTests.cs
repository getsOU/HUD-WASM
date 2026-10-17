using BasicHud.Core;
using SOU.Live.Contracts;

static class WeaponPresenceTests
{
    public static void Run(Action<bool, string> check)
    {
        var rifle = new WeaponState("rifle", "weapon_ak47", WeaponCategory.Rifle, WeaponStatus.Active, null, 7, 30, 61);
        var player = new PlayerState("player", "Player", TeamSide.CounterTerrorists, null, 100) { Weapons = [rifle] };
        var dead = player with { Health = 0 };
        check(EquipmentPresentation.Active(dead) is null && PlayerPresentation.Weapons(dead).Count == 0, "known death suppresses stale active weapon and inventory rather than rendering yesterday's held weapon");
        check(EquipmentPresentation.Active(player with { Weapons = [] }) is null && EquipmentPresentation.Active(player with { Weapons = null }) is null, "an explicit empty or unavailable inventory has no fabricated active weapon");
        check(EquipmentPresentation.Active(player with { Weapons = [rifle with { State = WeaponStatus.Holstered }] }) is null, "a holstered rifle is never substituted for the dropped active weapon");
        check(EquipmentPresentation.Active(player with { Weapons = [rifle with { State = WeaponStatus.Reloading }] })?.State == WeaponStatus.Reloading && EquipmentPresentation.Active(player with { Health = null }) == rifle, "reloading is an actual held weapon and unknown health is not mistaken for death");
        var unknown = rifle with { Name = null, Kind = WeaponKind.Unknown, Type = WeaponCategory.Unknown };
        check(ReferenceEquals(EquipmentPresentation.Active(player with { Weapons = [unknown] }), unknown) && EquipmentPresentation.Label(unknown.Name) == "Unknown weapon", "a supplied active weapon with unknown identity remains distinct from no active weapon");
        var game = new GameState("de_dust2", MatchPhase.Live, 1, RoundPhase.Live, new("CT", 0), new("T", 0), dead, [dead]);
        var observed = HudView.From(new(2, Guid.NewGuid(), 1, null, DataFreshness.Live, game)).Observed;
        check(observed is { Weapon: "", Ammo: "" }, "the shared view model also clears dead-pawn weapon text and stale ammunition");
        var unarmed = player with { Weapons = [] };
        var aliveView = HudView.From(new(2, Guid.NewGuid(), 1, null, DataFreshness.Live, game with { ObservedPlayer = unarmed, Players = [unarmed] })).Observed;
        check(aliveView is { Weapon: "", Ammo: "" }, "the shared view model leaves an alive unarmed weapon area empty instead of displaying unknown ammunition");
    }
}
