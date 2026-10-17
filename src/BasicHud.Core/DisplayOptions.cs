using System.Text.Json;
using SOU.Live.Contracts;

namespace BasicHud.Core;

public sealed record DisplayOptions(string Caption, bool ShowRosters, bool ShowObservedPlayer)
{
    public static DisplayOptions Default { get; } = new("sOU BASIC", true, true);

    public static DisplayOptions FromPanel(HudPanelState state) => new(
        state.Text("display", "caption")?.Trim() is { Length: > 0 } caption ? caption : Default.Caption,
        ReadVisible(state, "showRosters"),
        ReadVisible(state, "showObservedPlayer"));

    // Flag() returns false for missing values. An unset checkbox means our visible default.
    private static bool ReadVisible(HudPanelState state, string name) =>
        !state.Values.TryGetValue("display", out var section) ||
        !section.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.False;
}
