namespace BasicHud.Core;

/// <summary>Original CS2 artwork only; rights and file hashes ship in assets/valve-assets.json.</summary>
public static class Cs2Icons
{
    public static string? Status(string? name) => name switch
    {
        "health" => "ui/health",
        "kills" => "ui/crosshair_circle",
        "armor" => "ui/armor",
        "helmet" => "weapons/armor_helmet",
        "kit" => "ui/defuser_white",
        "bomb" => "weapons/c4",
        "dead" => "killfeed/icon_suicide",
        "headshot" => "killfeed/icon_headshot",
        "wallbang" => "killfeed/penetrate",
        "blind" => "killfeed/blind_kill",
        "smoke" => "killfeed/smoke_kill",
        "noscope" => "killfeed/noscope",
        "flashassist" => "weapons/flashbang_assist",
        "fire" => "weapons/inferno",
        "time" => "ui/clock",
        "hostage" => "ui/hostage_rescued",
        _ => null
    };
}
