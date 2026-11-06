namespace BasicHud.Presentation;

/// <summary>Shared semantic outline icons. Firearms, knives and C4 retain their original CS2 artwork.</summary>
public static class HudUiIcons
{
    public static string? Symbol(string? name) => name switch
    {
        "health" or "kills" or "dead" or "armor" or "helmet" or "kit" => name,
        "headshot" or "wallbang" or "noscope" or "blind" or "smoke" or "flashassist" or "fire" or "time" or "hostage" => name,
        "rounds" or "magazine" => name,
        "utility-hegrenade" or "utility-flashbang" or "utility-smokegrenade" or "utility-decoy" or "utility-molotov" or "utility-incgrenade" => name,
        _ => null
    };

    public static string? Utility(string? name) => EquipmentPresentation.Key(name) switch
    {
        var key when key is "hegrenade" or "flashbang" or "smokegrenade" or "decoy" or "molotov" or "incgrenade" => "utility-" + key,
        _ => null
    };
}
