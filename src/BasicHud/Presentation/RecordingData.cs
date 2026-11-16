using System.Collections.Immutable;
using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public static class RecordingData
{
    public static readonly (string Route, string Title)[] Views = [
        ("team-economy", "Team Economy"), ("round-compare", "Loadout Comparison"),
        ("side-statistics", "CT/T Statistics"), ("multikills", "Multikills"),
        ("round-buys", "Round Loadouts"), ("economy-evidence", "Economy Review")];

    public static bool IsView(string view) => Views.Any(v => v.Route == view);
    public static string Status(HudRoundHistoryStatus status) => status switch
    {
        HudRoundHistoryStatus.Available => "Recorded data",
        HudRoundHistoryStatus.NotRecorded => "No recording",
        HudRoundHistoryStatus.Unavailable => "Recording unavailable",
        _ => "Unknown recording status"
    };
    public static string Value(long? number) => number?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
    public static string Flag(bool? flag) => flag is null ? "Unknown" : flag.Value ? "Yes" : "No";
    public static string Delta(int? before, int? after) => before is { } a && after is { } b ? (b - a).ToString("+0;-0;0") : "Unknown";
    public static string Side(TeamSide? side) => side switch { TeamSide.CounterTerrorists => "CT", TeamSide.Terrorists => "T", _ => "Unassigned" };
    public static string Phase(HudRoundCheckpointKind kind) => kind switch
    {
        HudRoundCheckpointKind.FreezeStart => "First received freeze",
        HudRoundCheckpointKind.FreezeLast => "Last received freeze",
        HudRoundCheckpointKind.LiveStart => "Received round start",
        HudRoundCheckpointKind.RoundEnd => "Received round end",
        _ => "Unknown phase"
    };
    public static string Evidence(HudEvidence evidence) => evidence switch { HudEvidence.Confirmed => "Confirmed", HudEvidence.Derived => "Derived", _ => "Unknown" };
    public static string Inventory(ImmutableArray<WeaponState>? weapons) => weapons is null || weapons.Value.IsDefault ? "Unknown" : weapons.Value.Length == 0 ? "Empty inventory" :
        string.Join(" · ", weapons.Value.Select(w => $"{BroadcastContext.WeaponName(w)} [{Value(w.AmmoClip)} / {Value(w.AmmoReserve)}]"));

    public sealed record EconomyPoint(int Round, int? Value, int Observed, int Known);
    // FreezeLast is mutable during freeze. It becomes an end-of-freeze sample only
    // after a later checkpoint in this recording proves the phase has ended.
    public static HudRoundCheckpoint? Checkpoint(HudRecordedRound? round, HudRoundCheckpointKind phase)
    {
        var sample = round?.Checkpoints.FirstOrDefault(c => c.Kind == phase);
        if (sample is null || phase != HudRoundCheckpointKind.FreezeLast) return sample;
        return round!.Checkpoints.Any(c => c.Kind is HudRoundCheckpointKind.LiveStart or HudRoundCheckpointKind.RoundEnd
            && c.ReceivedAt >= sample.ReceivedAt) ? sample : null;
    }
    public static int EconomyEndRound(IEnumerable<HudRecordedRound> rounds, HudRoundCheckpointKind phase, string selection) =>
        RecordingOptions.RoundNumber(rounds.Where(r => Checkpoint(r, phase) is not null).Select(r => r.Number), selection);
    public static IReadOnlyList<EconomyPoint> Points(IEnumerable<HudRecordedRound> rounds, HudRoundCheckpointKind phase, string? teamId, TeamSide? side, bool equipment)
    {
        var source = rounds.ToDictionary(r => r.Number);
        if (source.Count == 0) return [];
        return Enumerable.Range(source.Keys.Min(), source.Keys.Max() - source.Keys.Min() + 1).Select(number =>
        {
            var checkpoint = Checkpoint(source.GetValueOrDefault(number), phase);
            var players = checkpoint?.Players.Where(p => teamId is not null
                ? (p.TeamId ?? (p.Side == TeamSide.CounterTerrorists ? checkpoint.CtTeamId : p.Side == TeamSide.Terrorists ? checkpoint.TTeamId : null)) == teamId
                : p.Side == side).ToArray() ?? [];
            var values = players.Select(p => equipment ? p.EquipmentValue : p.Money).ToArray();
            return new EconomyPoint(number, values.Length > 0 && values.All(v => v.HasValue) ? values.Sum(v => v!.Value) : null, players.Length, values.Count(v => v.HasValue));
        }).ToArray();
    }
}
