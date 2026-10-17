using System.Text.Json;
using SOU.Live.Contracts;

namespace BasicHud.Core;

public sealed record DisplayOptions(string Caption, bool ShowRosters, bool ShowObservedPlayer)
{
    public static DisplayOptions Default { get; } = new("sOU BASIC", true, true);

    public static DisplayOptions FromPanel(HudPanelState state) => new(
        state.Text("display", "caption")?.Trim() is { Length: > 0 } caption ? caption : Default.Caption,
        ReadVisible(state, "hideRosters"),
        ReadVisible(state, "hideObservedPlayer"));

    // The host normalizes unset checkboxes to false. Opt-out fields keep the HUD visible
    // both before the first save and when an unrelated caption change is saved.
    private static bool ReadVisible(HudPanelState state, string name) =>
        !state.Values.TryGetValue("display", out var section) ||
        !section.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.True;
}
