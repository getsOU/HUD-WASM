using System.Text.Json;
using BasicHud.Presentation;
using SOU.Live.Contracts;

static class PortraitAlignmentTests
{
    public static void Run(Action<bool, string> check)
    {
        HudPanelState Panel(object? value) => new(1, new() { ["display"] = new() { ["portraitAlignment"] = JsonSerializer.SerializeToElement(value) } });
        check(DisplayOptions.PortraitPosition(null) == "center top" && DisplayOptions.PortraitPosition(new(0, [])) == "center top",
            "old and unset panels keep player portraits anchored at the top by default");
        var positions = new Dictionary<string, string>
        {
            ["top"] = "center top",
            ["top-left"] = "left top",
            ["top-right"] = "right top",
            ["left"] = "left center",
            ["center"] = "center center",
            ["right"] = "right center",
            ["bottom-left"] = "left bottom",
            ["bottom"] = "center bottom",
            ["bottom-right"] = "right bottom"
        };
        check(positions.All(pair => DisplayOptions.PortraitPosition(Panel(pair.Key)) == pair.Value), "all nine operator portrait alignments map to fixed CSS positions");
        check(new object?[] { null, true, 1, "", "invalid", "top; background:url(https://invalid)" }.All(value => DisplayOptions.PortraitPosition(Panel(value)) == "center top"),
            "malformed portrait alignment cannot inject styles and falls back to top center");
    }
}
