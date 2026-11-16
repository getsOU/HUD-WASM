using SOU.Live.Contracts;

namespace BasicHud.Presentation;

/// <summary>Operator settings live in the host panel, never in the output.</summary>
public sealed record RecordingOptions(string Section, bool Equipment, HudRoundCheckpointKind Phase,
    string RoundA, string RoundB, HudRoundCheckpointKind PhaseA, HudRoundCheckpointKind PhaseB,
    string Round, int Window, int EventPage, string? PlayerId, string Samples, int InventoryPage = 1)
{
    public static string SectionFor(string view) => view switch
    {
        "team-economy" => "recordingEconomy",
        "round-compare" => "recordingCompare",
        "side-statistics" => "recordingSides",
        "multikills" => "recordingMultikills",
        "round-buys" => "recordingBuys",
        _ => "recordingEvidence"
    };
    public static RecordingOptions From(BroadcastContext context, string view)
    {
        var section = SectionFor(view);
        string Text(string name) => context.Text(section, name);
        int Positive(string name, int fallback, int max) => int.TryParse(Text(name), out var value) && value > 0 && value <= max ? value : fallback;
        var player = context.Panel?.References.TryGetValue(section, out var fields) == true && fields.TryGetValue("player", out var reference) && reference.Missing != true
            ? reference.Id : null;
        return new(section, Text("metric") == "equipment", Checkpoint(Text("phase")), Text("roundA"), Text("roundB"),
            Checkpoint(Text("phaseA")), Checkpoint(Text("phaseB")), Text("round"), Positive("window", 12, 64),
            Positive("eventPage", 1, 1024), player,
            Text("samples") is "ct" or "t" or "unassigned" ? Text("samples") : "all", Positive("inventoryPage", 1, 1024));
    }
    public static HudRoundCheckpointKind Checkpoint(string value) => value switch
    {
        "freezeStart" => HudRoundCheckpointKind.FreezeStart,
        "liveStart" => HudRoundCheckpointKind.LiveStart,
        "roundEnd" => HudRoundCheckpointKind.RoundEnd,
        _ => HudRoundCheckpointKind.FreezeLast
    };
    public static int RoundNumber(IEnumerable<int> numbers, string selection, bool previous = false)
    {
        var ordered = numbers.Distinct().Order().ToArray();
        if (int.TryParse(selection, out var number) && number > 0 && number <= 10000) return number;
        if (ordered.Length == 0) return 0;
        return selection == "previous" || previous && string.IsNullOrWhiteSpace(selection) ? ordered[Math.Max(0, ordered.Length - 2)] : ordered[^1];
    }
    public static IReadOnlyList<T> PageOf<T>(IReadOnlyList<T> items, int requested, int size) => items.Skip((PageNumber(items.Count, requested, size) - 1) * size).Take(size).ToArray();
    public static int Pages(int count, int size) => Math.Max(1, (count + size - 1) / size);
    public static int PageNumber(int count, int requested, int size) => Math.Clamp(requested, 1, Pages(count, size));
}

public static class RecordingCast
{
    public static TeamSide? DisplaySide(BroadcastContext context, string? teamId, TeamSide? historicalSide) =>
        string.IsNullOrWhiteSpace(teamId) ? historicalSide : context.CurrentTeamSide(teamId);
    public static string? CheckpointTeamId(HudRoundCheckpoint? point, TeamSide? side) => side switch
    {
        TeamSide.CounterTerrorists => point?.CtTeamId,
        TeamSide.Terrorists => point?.TTeamId,
        _ => null
    };
    public static string? PlayerTeamId(HudRecordedRound? round, string steamId)
    {
        var ids = (round?.Checkpoints ?? []).SelectMany(c => c.Players.Where(p => p.SteamId == steamId)
            .Select(p => p.TeamId ?? CheckpointTeamId(c, p.Side)))
            .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToArray();
        return ids.Length == 1 ? ids[0] : null;
    }
    public static string? AnalysisPlayerTeamId(HudMapAnalysis analysis, string steamId) => analysis.Rounds.OrderByDescending(r => r.Number)
        .SelectMany(r => r.Players).FirstOrDefault(p => p.SteamId == steamId)?.TeamId;
    public static string EconomyColor(TeamSide? side, int index) => side switch
    {
        TeamSide.CounterTerrorists => "var(--ct)",
        TeamSide.Terrorists => "var(--t)",
        _ => index == 0 ? "#bdc8d7" : "#8d9eb5"
    };
    // Only recorded identities from this round may name its historical side.
    // Current CT/T recognition can refer to the opposite team after halftime.
    public static string? TeamId(HudRecordedRound? round, TeamSide side)
    {
        var ids = (round?.Checkpoints ?? []).SelectMany(c =>
            c.Players.Where(p => p.Side == side).Select(p => p.TeamId)
                .Append(side == TeamSide.CounterTerrorists ? c.CtTeamId : c.TTeamId))
            .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToArray();
        return ids.Length == 1 ? ids[0] : null;
    }
    public static string Number(long? value) => RecordingCopy.Instance.Number(value);
    public static string Decimal(double? value) => RecordingCopy.Instance.Decimal(value);
    public static string Money(long? value) => RecordingCopy.Instance.Money(value);
    public static string Flag(bool? value) => RecordingCopy.Instance.Flag(value);
    public static string Side(TeamSide? side) => RecordingCopy.Instance.Side(side);
    public static string SideClass(TeamSide? side) => side switch { TeamSide.CounterTerrorists => "ct", TeamSide.Terrorists => "t", _ => "neutral" };
    public static string Phase(HudRoundCheckpointKind phase) => RecordingCopy.Instance.Phase(phase);
    public static string Buy(HudBuyClass value) => RecordingCopy.Instance.Buy(value);
    public static string Force(HudForceBuyKind value) => RecordingCopy.Instance.Force(value);
    public static string Team(BroadcastContext context, string? id, TeamSide? side = null) => RecordingCopy.Instance.Team(context, id, side);
    public static string? Logo(BroadcastContext context, string? id) => context.ResolveMedia(id is not null && id == context.Match?.TeamA.Id ? context.Match.TeamA.Logo
        : id is not null && id == context.Match?.TeamB.Id ? context.Match.TeamB.Logo : null);
    public static string Delta(int? before, int? after) => RecordingCopy.Instance.Delta(before, after);
}
