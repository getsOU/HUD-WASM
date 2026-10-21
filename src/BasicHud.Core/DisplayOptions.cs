using System.Text.Json;
using SOU.Live.Contracts;

namespace BasicHud.Core;

public sealed record DisplayOptions(string Caption, bool ShowRosters, bool ShowObservedPlayer)
{
    public string Scene { get; init; } = "live";
    public bool ShowRadar { get; init; } = true;
    public bool ShowKillfeed { get; init; } = true;
    public bool ShowEconomy { get; init; } = true;
    public bool ShowRoundHistory { get; init; } = true;
    public bool ShowSeries { get; init; } = true;
    public bool ShowSponsors { get; init; } = true;
    public bool ShowPlayerImages { get; init; } = true;
    public static DisplayOptions Default { get; } = new("", true, true);

    public static DisplayOptions FromPanel(HudPanelState state) => new(
        state.Text("display", "caption")?.Trim() is { Length: > 0 } caption ? caption : Default.Caption,
        ReadVisible(state, "hideRosters"),
        ReadVisible(state, "hideObservedPlayer"))
    {
        Scene = SceneSelection.Valid(state.Text("display", "scene")),
        ShowRadar = ReadVisible(state, "hideRadar"),
        ShowKillfeed = ReadVisible(state, "hideKillfeed"),
        ShowEconomy = ReadVisible(state, "hideEconomy"),
        ShowRoundHistory = ReadVisible(state, "hideRoundHistory"),
        ShowSeries = ReadVisible(state, "hideSeries"),
        ShowSponsors = ReadVisible(state, "hideSponsors"),
        ShowPlayerImages = PlayerImagesVisible(state)
    };

    public static bool PlayerImagesVisible(HudPanelState? state) => state is null || ReadVisible(state, "agentsOnly");

    // The host normalizes unset checkboxes to false. Opt-out fields keep the HUD visible
    // both before the first save and when an unrelated caption change is saved.
    private static bool ReadVisible(HudPanelState state, string name) =>
        !state.Values.TryGetValue("display", out var section) ||
        !section.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.True;
}
