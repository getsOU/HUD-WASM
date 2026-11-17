using System.Text.Json;
using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public sealed record DisplayOptions(string Caption, bool ShowRosters, bool ShowObservedPlayer)
{
    public string Scene { get; init; } = "live";
    public bool ShowRadar { get; init; } = true;
    public bool ShowKillfeed { get; init; } = true;
    public bool ShowTrades { get; init; } = true;
    public bool ShowRoundHistory { get; init; } = true;
    public bool ShowSeries { get; init; } = true;
    public bool ShowSponsors { get; init; } = true;
    public bool ShowPlayerImages { get; init; } = true;
    public bool HorizontalPlayercards { get; init; }
    public static DisplayOptions Default { get; } = new("", true, true);

    public static DisplayOptions FromPanel(HudPanelState state, string? defaultCaption = null) => new(
        state.Text("display", "caption")?.Trim() ?? defaultCaption ?? Default.Caption,
        ReadVisible(state, "hideRosters"),
        ReadVisible(state, "hideObservedPlayer"))
    {
        Scene = SceneSelection.Valid(state.Text("display", "scene")),
        ShowRadar = ReadVisible(state, "hideRadar"),
        ShowKillfeed = ReadVisible(state, "hideKillfeed"),
        ShowTrades = ReadVisible(state, "hideTrades"),
        ShowRoundHistory = ReadVisible(state, "hideRoundHistory"),
        ShowSeries = ReadVisible(state, "hideSeries"),
        ShowSponsors = ReadVisible(state, "hideSponsors"),
        ShowPlayerImages = PlayerImagesVisible(state),
        HorizontalPlayercards = !ReadVisible(state, "horizontalPlayercards")
    };

    public static bool PlayerImagesVisible(HudPanelState? state) => state is null || ReadVisible(state, "agentsOnly");
    public static string PortraitPosition(HudPanelState? state) => state?.Text("display", "portraitAlignment") switch
    {
        "top-left" => "left top",
        "top-right" => "right top",
        "left" => "left center",
        "center" => "center center",
        "right" => "right center",
        "bottom-left" => "left bottom",
        "bottom" => "center bottom",
        "bottom-right" => "right bottom",
        _ => "center top"
    };

    // The host normalizes unset checkboxes to false. Opt-out fields keep the HUD visible
    // both before the first save and when an unrelated caption change is saved.
    private static bool ReadVisible(HudPanelState state, string name) =>
        !state.Values.TryGetValue("display", out var section) ||
        !section.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.True;
}
