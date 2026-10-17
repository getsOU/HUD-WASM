using SOU.Live.Contracts;

namespace BasicHud.Core;

public sealed record UtilityItem(string Name, string Label, int? Count);
public static class EquipmentPresentation
{
    public static int UtilityCopies(int? count) => count is >= 0 and <= 10 ? count.Value : 1;
    public static WeaponState? Active(PlayerState player) => PlayerPresentation.Weapons(player).FirstOrDefault(weapon => weapon.State is WeaponStatus.Active or WeaponStatus.Reloading);
    public static string? Key(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = name.Trim().ToLowerInvariant();
        if (key.StartsWith("weapon_", StringComparison.Ordinal)) key = key[7..];
        return key.Length is > 0 and <= 64 && key.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_') ? key : null;
    }
    public static string Label(string? name) => Key(name) switch
    {
        "ak47" => "AK-47",
        "m4a1" => "M4A4",
        "m4a1_silencer" => "M4A1-S",
        "usp_silencer" => "USP-S",
        "hkp2000" => "P2000",
        "elite" => "Dual Berettas",
        "ssg08" => "SSG 08",
        "g3sg1" => "G3SG1",
        "scar20" => "SCAR-20",
        "mp5sd" => "MP5-SD",
        "cz75a" => "CZ75-Auto",
        "revolver" => "R8",
        "hegrenade" => "HE grenade",
        "flashbang" => "Flashbang",
        "smokegrenade" => "Smoke",
        "incgrenade" => "Incendiary",
        "molotov" => "Molotov",
        "decoy" => "Decoy",
        "c4" => "C4",
        null => "Unknown weapon",
        var key when key.StartsWith("knife", StringComparison.Ordinal) => "Knife",
        var key => key.Replace('_', ' ').ToUpperInvariant()
    };
    public static bool UsesAmmo(WeaponState? weapon) => weapon?.Type is WeaponCategory.Pistol or WeaponCategory.SubmachineGun or WeaponCategory.Rifle or WeaponCategory.SniperRifle or WeaponCategory.Shotgun or WeaponCategory.MachineGun;
    public static string Ammo(WeaponState? weapon) => UsesAmmo(weapon) ? $"{BroadcastContext.Number(weapon!.AmmoClip)} / {BroadcastContext.Number(weapon.AmmoReserve)}" : "";
    public static IReadOnlyList<UtilityItem> Utility(PlayerState player) => PlayerPresentation.Weapons(player)
        .Where(weapon => weapon.Type == WeaponCategory.Grenade && Key(weapon.Name) is not null)
        .GroupBy(weapon => Key(weapon.Name)!, StringComparer.Ordinal)
        .Select(group => new UtilityItem(group.Key, Label(group.Key), group.Any(weapon => weapon.AmmoReserve is null or < 0 or > 10) ? null : group.Sum(weapon => weapon.AmmoReserve!.Value)))
        .OrderBy(item => item.Name, StringComparer.Ordinal).Take(6).ToArray();
    public static bool HasBomb(PlayerState player, GameState? game) => player.Health is not <= 0 &&
        (game?.Bomb?.PlayerSteamId is { } id && id == player.SteamId && game.Bomb.State is BombStatus.Carried or BombStatus.Planting || PlayerPresentation.Weapons(player).Any(weapon => weapon.Type == WeaponCategory.Bomb));
}
