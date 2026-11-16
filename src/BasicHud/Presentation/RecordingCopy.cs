using System.Globalization;
using SOU.Live.Contracts;

namespace BasicHud.Presentation;

/// <summary>English broadcast copy and consistent number formats.</summary>
public sealed class RecordingCopy
{
    public static RecordingCopy Instance { get; } = new();
    private RecordingCopy() { }
    public CultureInfo Culture => CultureInfo.InvariantCulture;
    public string this[string key] => Copy.TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);
    public static IReadOnlyCollection<string> Keys => Copy.Keys;
    private static readonly Dictionary<string, string> Copy = new(StringComparer.Ordinal)
    {
        ["title.team-economy"] = "TEAM ECONOMY",
        ["title.round-compare"] = "LOADOUT COMPARISON",
        ["title.side-statistics"] = "CT / T STATISTICS",
        ["title.multikills"] = "MULTIKILLS",
        ["title.round-buys"] = "ROUND LOADOUTS",
        ["title.economy-evidence"] = "ECONOMY REVIEW",
        ["example"] = "EXAMPLE DATA",
        ["unknown"] = "UNKNOWN",
        ["unknownName"] = "Unknown",
        ["mapUnknown"] = "MAP UNKNOWN",
        ["teamUnknown"] = "TEAM UNKNOWN",
        ["yes"] = "YES",
        ["no"] = "NO",
        ["unassigned"] = "UNASSIGNED",
        ["ctSide"] = "CT SIDE",
        ["tSide"] = "T SIDE",
        ["money"] = "MONEY",
        ["equipment"] = "EQUIPMENT",
        ["equipmentValue"] = "EQUIPMENT VALUE",
        ["armor"] = "ARMOR",
        ["helmet"] = "HELMET",
        ["kit"] = "KIT",
        ["round"] = "ROUND",
        ["players"] = "PLAYERS",
        ["events"] = "EVENTS",
        ["items"] = "ITEMS",
        ["samples"] = "SAMPLES",
        ["weapons"] = "WEAPONS",
        ["observedPlayers"] = "OBSERVED PLAYERS",
        ["observedBuyers"] = "OBSERVED BUYERS",
        ["observedEvents"] = "OBSERVED EVENTS",
        ["change"] = "CHANGE",
        ["phase.first"] = "FIRST FREEZE",
        ["phase.last"] = "FREEZE END",
        ["phase.live"] = "ROUND START",
        ["phase.end"] = "ROUND END",
        ["missing"] = "MISSING",
        ["freeze"] = "FREEZE",
        ["live"] = "LIVE",
        ["end"] = "END",
        ["phaseUnknown"] = "PHASE ?",
        ["buy.eco"] = "ECO",
        ["buy.light"] = "LIGHT",
        ["buy.full"] = "FULL",
        ["buy.mixed"] = "MIXED",
        ["force.likely"] = "LIKELY FORCE BUY",
        ["force.none"] = "NO FORCE-BUY SIGNAL",
        ["force.unknown"] = "FORCE BUY —",
        ["confirmed"] = "CONFIRMED",
        ["derived"] = "DERIVED",
        ["noSample"] = "NO SAMPLE",
        ["killRounds"] = "KILL ROUNDS",
        ["rounds"] = "ROUNDS",
        ["partial"] = "PARTIAL",
        ["headshotKills"] = "HS KILLS",
        ["kills"] = "KILLS",
        ["mapStats"] = "MAP STATISTICS",
        ["multiRounds"] = "MULTIKILL ROUNDS",
        ["sidePerformance"] = "CT / T PERFORMANCE",
        ["noPlayerValues"] = "NO PLAYER DATA",
        ["noPlayerStates"] = "NO PLAYER STATES",
        ["notRecorded"] = "NOT RECORDED",
        ["inventoryUnknown"] = "INVENTORY UNKNOWN",
        ["inventoryEmpty"] = "EMPTY INVENTORY",
        ["playerUnknown"] = "PLAYER STATE UNKNOWN",
        ["loadouts"] = "BUY CLASS & END INVENTORY",
        ["freezeEquipment"] = "FREEZE EQUIPMENT",
        ["survivors"] = "SURVIVORS",
        ["buyClass"] = "BUY CLASS",
        ["freezeToEnd"] = "FREEZE → END",
        ["endMissing"] = "END DATA MISSING",
        ["survived"] = "SURVIVED",
        ["dead"] = "DEAD",
        ["eventKinds"] = "PURCHASE · RECEIVED · REMOVAL",
        ["purchase"] = "PURCHASE",
        ["pickup"] = "ITEM RECEIVED",
        ["removal"] = "REMOVAL",
        ["buyer"] = "BUYER",
        ["picker"] = "RECEIVED BY",
        ["remover"] = "REMOVED BY",
        ["noEvents"] = "NO OBSERVED EVENTS",
        ["cashCarry"] = "CASH & CARRYOVER",
        ["netCash"] = "NET CASH CHANGE",
        ["purchases"] = "OBS. PURCHASES",
        ["pickups"] = "OBS. RECEIVED",
        ["removed"] = "OBS. REMOVALS",
        ["refund"] = "POSSIBLE REFUND",
        ["fromRound"] = "FROM ROUND",
        ["carryUnknown"] = "CARRYOVER —",
        ["recordPartial"] = "PARTIAL RECORD",
        ["noValues"] = "NO KNOWN VALUES",
        ["chartLabel"] = "Team economy by round",
        ["state.reconnecting"] = "RECONNECTING",
        ["state.loading"] = "LOADING ROUND DATA",
        ["state.waiting"] = "WAITING FOR MATCH",
        ["state.notrecorded"] = "NO RECORDING",
        ["state.unsupported"] = "RECORDING NOT SUPPORTED",
        ["state.unavailable"] = "RECORDING UNAVAILABLE",
        ["state.available"] = "NO ROUND DATA",
        ["state.error"] = "CONNECTION LOST",
    };
    public string Number(long? value) => value?.ToString("N0", Culture) ?? "—";
    public string Decimal(double? value) => value?.ToString("0.0", Culture) ?? "—";
    public string Money(long? value) => value is null ? "—" : "$" + Number(value);
    public string Flag(bool? value) => value is null ? "?" : this[value.Value ? "yes" : "no"];
    public string Delta(int? before, int? after) => before.HasValue && after.HasValue ? (after.Value - before.Value).ToString("+0;-0;0", Culture) : "—";
    public string Scope(int shown, int total, string noun) => (shown < total ? $"{Number(shown)} / {Number(total)}" : Number(total)) + " " + this[noun];
    public string Phase(HudRoundCheckpointKind phase) => this[phase switch { HudRoundCheckpointKind.FreezeStart => "phase.first", HudRoundCheckpointKind.LiveStart => "phase.live", HudRoundCheckpointKind.RoundEnd => "phase.end", _ => "phase.last" }];
    public string Side(TeamSide? side) => side switch { TeamSide.CounterTerrorists => "CT", TeamSide.Terrorists => "T", _ => this["unassigned"] };
    public string Buy(HudBuyClass value) => this[value switch { HudBuyClass.Eco => "buy.eco", HudBuyClass.Light => "buy.light", HudBuyClass.Full => "buy.full", HudBuyClass.Mixed => "buy.mixed", _ => "unknown" }];
    public string Force(HudForceBuyKind value) => this[value switch { HudForceBuyKind.ForceLikely => "force.likely", HudForceBuyKind.NoForceSignal => "force.none", _ => "force.unknown" }];
    public string Evidence(HudEvidence value) => this[value switch { HudEvidence.Confirmed => "confirmed", HudEvidence.Derived => "derived", _ => "unknown" }];
    public string Team(BroadcastContext context, string? id, TeamSide? side = null) => id is not null && id == context.Match?.TeamA.Id ? context.Match.TeamA.Name : id is not null && id == context.Match?.TeamB.Id ? context.Match.TeamB.Name : side is not null ? Side(side) : this["teamUnknown"];
    public string Compact(int value) => value >= 1000 ? (value / 1000.0).ToString("0.#", Culture) + "k" : Number(value);
    public string Weapon(string? name) => EquipmentPresentation.Label(name);
}
