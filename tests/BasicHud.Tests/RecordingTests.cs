using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using BasicHud.Presentation;
using SOU.Live.Contracts;



public static class RecordingTests
{
    public static Task Run(Action<bool, string> check)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "recording-showcase.json")));
        var raw = fixture.RootElement.GetProperty("recordingSnapshot").GetProperty("roundHistory").Deserialize<HudRoundHistory>(options)!;
        var analysis = fixture.RootElement.GetProperty("recordingSnapshot").GetProperty("mapAnalysis").Deserialize<HudMapAnalysis>(options)!;
        var firstPlayer = new PlayerState("recorded-player", "Player", TeamSide.Terrorists, null, null) { ObserverSlot = 3 };
        var game = new GameState("de_mirage", MatchPhase.Live, 6, RoundPhase.Live, null, null, firstPlayer, [firstPlayer]);
        var identityContext = BroadcastContext.Empty with { Snapshot = new(2, Guid.NewGuid(), 1, DateTimeOffset.UtcNow, DataFreshness.Live, game) };
        check(AgentPortraits.ForRecorded(identityContext, firstPlayer.SteamId) == AgentPortraits.For(firstPlayer),
            "recorded identities reuse the live agent portrait by Steam ID");
        check(AgentPortraits.ForRecorded(BroadcastContext.Empty, "historical", TeamSide.Terrorists)?.StartsWith("assets/agents/t-", StringComparison.Ordinal) == true
            && AgentPortraits.ForRecorded(BroadcastContext.Empty, "historical", TeamSide.Unknown) is null,
            "historical identities have stable side-correct decorative agents without assuming an unknown side");
        check(RecordingCast.TeamId(raw.Rounds[0], TeamSide.CounterTerrorists) == "example-alpha"
            && RecordingCast.TeamId(raw.Rounds[^1], TeamSide.CounterTerrorists) == "example-bravo",
            "historical team headers follow recorded team IDs through halftime rather than current recognition");
        check(RecordingCast.TeamId(null, TeamSide.CounterTerrorists) is null
            && RecordingCast.TeamId(raw.Rounds[0] with { Checkpoints = raw.Rounds[0].Checkpoints.Select(c => c with { CtTeamId = "conflicting" }).ToImmutableArray() }, TeamSide.CounterTerrorists) is null,
            "missing or contradictory recorded team IDs never borrow the current side's identity");
        var points = RecordingData.Points(raw.Rounds, HudRoundCheckpointKind.FreezeLast, "example-alpha", null, false);
        check(points[0].Value == 0 && points[1].Value == 1600 && points[3].Value == 4500, "economy follows team IDs through halftime and preserves real zero");
        check(points[2].Value is null && points[2].Known == 1 && points[2].Observed == 2, "incomplete observed team sums remain gaps with explicit sample counts");
        var missingRound = RecordingData.Points(raw.Rounds.Where(r => r.Number != 2), HudRoundCheckpointKind.FreezeLast, "example-alpha", null, false);
        check(missingRound[1].Round == 2 && missingRound[1].Value is null, "absent rounds create chart gaps rather than bridging lines");
        var lastRound = raw.Rounds[^1];
        var pendingFreeze = lastRound with
        {
            Number = 7,
            Status = HudRecordedRoundStatus.Recording,
            Checkpoints = lastRound.Checkpoints.Where(c => c.Kind is HudRoundCheckpointKind.FreezeStart or HudRoundCheckpointKind.FreezeLast).ToImmutableArray()
        };
        var withPending = raw.Rounds.Add(pendingFreeze);
        check(RecordingData.Points(withPending, HudRoundCheckpointKind.FreezeLast, "example-alpha", null, true)[^1].Value is null
            && RecordingData.EconomyEndRound(withPending, HudRoundCheckpointKind.FreezeLast, "") == 6,
            "an ongoing freeze never contributes a freeze-end graph point or replaces the latest completed headline");
        var changedFreeze = pendingFreeze with
        {
            Checkpoints = pendingFreeze.Checkpoints.Select(c => c.Kind != HudRoundCheckpointKind.FreezeLast ? c : c with
            {
                Players = c.Players.Select(p => p with { EquipmentValue = 123456 }).ToImmutableArray()
            }).ToImmutableArray()
        };
        check(RecordingData.Checkpoint(changedFreeze, HudRoundCheckpointKind.FreezeLast) is null
            && RecordingData.Checkpoint(changedFreeze, HudRoundCheckpointKind.FreezeStart) is not null,
            "changing provisional freeze values remain hidden while first-freeze selection remains usable");
        var started = pendingFreeze with { Checkpoints = pendingFreeze.Checkpoints.Add(lastRound.Checkpoints.Single(c => c.Kind == HudRoundCheckpointKind.LiveStart)) };
        check(RecordingData.Checkpoint(started, HudRoundCheckpointKind.FreezeLast) is not null
            && RecordingData.EconomyEndRound(raw.Rounds.Add(started), HudRoundCheckpointKind.FreezeLast, "") == 7,
            "the saved round-start checkpoint makes the last freeze sample eligible");
        var ended = pendingFreeze with { Checkpoints = pendingFreeze.Checkpoints.Add(lastRound.Checkpoints.Single(c => c.Kind == HudRoundCheckpointKind.RoundEnd)) };
        check(RecordingData.Checkpoint(ended, HudRoundCheckpointKind.FreezeLast) is not null
            && RecordingData.Checkpoint(pendingFreeze with { Status = HudRecordedRoundStatus.Ended }, HudRoundCheckpointKind.FreezeLast) is null,
            "a saved round-end checkpoint can close freeze, but a status flag alone cannot invent a phase boundary");
        var staleBoundary = started with
        {
            Checkpoints = started.Checkpoints.Select(c => c.Kind == HudRoundCheckpointKind.LiveStart ? c with
            {
                ReceivedAt = pendingFreeze.Checkpoints.Single(p => p.Kind == HudRoundCheckpointKind.FreezeLast).ReceivedAt.AddSeconds(-1)
            } : c).ToImmutableArray()
        };
        check(RecordingData.Checkpoint(staleBoundary, HudRoundCheckpointKind.FreezeLast) is null
            && RecordingData.EconomyEndRound(withPending, HudRoundCheckpointKind.FreezeLast, "7") == 7,
            "an older boundary cannot finalize a later freeze sample; an explicitly selected pending round stays unknown");
        check(RecordingData.Inventory(null) == "Unknown" && RecordingData.Inventory([]) == "Empty inventory" && RecordingData.Value(0) == "0", "unknown inventory, empty inventory and real zero stay distinct");
        check(RecordingData.Delta(null, 0) == "Unknown" && RecordingData.Delta(0, 0) == "0" && RecordingData.Delta(0, 12) == "+12", "comparison deltas require both observed values");
        var sample = analysis.Players.First(p => p.SteamId == raw.Rounds[0].Checkpoints[0].Players[0].SteamId && p.Side == TeamSide.CounterTerrorists);
        check(sample.DoubleKills == 1 && sample.TripleKills == 1 && sample.KillRounds == 2 && sample.HeadshotRounds == 1 && sample.HeadshotPercentage == 50, "portable example demonstrates exclusive multikills and independent headshot coverage");
        check(RecordingData.Views.All(v => SceneSelection.Resolve("views/" + v.Route, "live") == v.Route), "all six recording routes resolve through the existing view manager");
        var defaults = RecordingOptions.From(BroadcastContext.Empty, "round-compare");
        check(defaults.PhaseA == HudRoundCheckpointKind.FreezeLast && defaults.PhaseB == HudRoundCheckpointKind.FreezeLast && defaults.InventoryPage == 1,
            "broadcast panel defaults use the last received freeze and the first inventory page");
        check(RecordingOptions.RoundNumber([1, 2, 5], "") == 5 && RecordingOptions.RoundNumber([1, 2, 5], "", previous: true) == 2
            && RecordingOptions.RoundNumber([1, 2, 5], "3") == 3, "automatic round selection follows saved rounds while explicit missing rounds remain selected");
        check(new[] { 0, 4, 10, 13, 32, 64 }.All(count => { var layout = RecordingLayout.Cards(count); return layout.Columns * layout.Rows >= count; }),
            "adaptive layouts allocate a visible cell for every observed player without player pages");
        var totals = RecordedMultikills.From(analysis.Players);
        var lead = totals.First(p => p.SteamId == sample.SteamId);
        check(totals.Count == analysis.Players.Select(p => p.SteamId).Distinct().Count() && lead.DoubleKills == 1 && lead.TripleKills == 1
            && lead.QuadKills == 1 && lead.FivePlusKills == 1 && lead.KillRounds == 5 && lead.Rounds == 6,
            "multikills join Steam IDs and combine exclusive CT, T and unassigned samples into one row per player");
        var uncovered = RecordedMultikills.From(analysis.Players.Where(p => p.SteamId == sample.SteamId).Select(p => p with
        {
            KillRounds = 0,
            Kills = null,
            DoubleKills = 0,
            TripleKills = 0,
            QuadKills = 0,
            FivePlusKills = 0
        })).Single();
        check(uncovered.KillRounds == 0 && uncovered.DoubleKills is null && uncovered.FivePlusKills is null,
            "combined multikills remain unknown when no kill-round sample is covered");
        check(SourceText.Name("Wank&apos;ePEW") == "Wank'ePEW" && SourceText.Name("A &amp; B &#x1F680;") == "A & B 🚀"
            && SourceText.Name("&amp;apos;") == "&apos;" && SourceText.Name("&lt;b&gt;Name&lt;/b&gt;") == "<b>Name</b>",
            "recorded source names use the existing one-pass entity decoding while remaining plain text");
        var panel = new HudPanelState(1, JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>("""
            {"recordingEconomy":{"metric":"equipment","phase":"roundEnd","round":"3","window":"6"},
             "recordingCompare":{"roundA":"1","roundB":"2","phaseA":"freezeStart","phaseB":"liveStart","page":"2","inventoryPage":"3"}}
            """)!);
        var settingsContext = BroadcastContext.Empty with { Panel = panel };
        var economySettings = RecordingOptions.From(settingsContext, "team-economy");
        var compareSettings = RecordingOptions.From(settingsContext, "round-compare");
        check(economySettings.Equipment && economySettings.Phase == HudRoundCheckpointKind.RoundEnd && economySettings.Window == 6
            && compareSettings.RoundA == "1" && compareSettings.RoundB == "2" && compareSettings.PhaseB == HudRoundCheckpointKind.LiveStart
            && compareSettings.InventoryPage == 3, "independent panel sections configure each broadcast without controls in the output");
        var schema = JsonSerializer.Deserialize<HudPanelSection[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "panel.json")), options)!;
        check(RecordingData.Views.All(v => schema.Any(s => s.Name == RecordingOptions.SectionFor(v.Route)
            && s.Inputs.Any(i => i.Name == "refresh" && i.Type == HudPanelFieldType.Action) && s.Inputs.Any(i => i.Name == "title"))),
            "every recording broadcast has a dedicated host panel with an explicit refresh action");
        check(RecordingCast.Money(0) == "$0" && RecordingCast.Money(null) == "—" && RecordingCast.Flag(false) == "NO"
            && RecordingCast.Flag(null) == "?" && RecordingCast.Delta(null, 0) == "—", "compact broadcast labels preserve zero, false and unknown instead of inventing values");
        check(RecordingCopy.Instance["ctSide"] == "CT SIDE" && RecordingCopy.Instance["tSide"] == "T SIDE"
            && RecordingCopy.Instance["phase.last"] == "FREEZE END" && RecordingCopy.Instance["confirmed"] == "CONFIRMED",
            "English viewer labels identify sides and evidence without technical explanations");
        check(RecordingCopy.Instance["pickup"] == "ITEM RECEIVED" && RecordingCopy.Instance["picker"] == "RECEIVED BY",
            "native inventory events do not claim a physical pickup from the ground");
        check(schema.All(s => s.Inputs.All(i => i.Name != "recordingLanguage"))
            && RecordingCopy.Keys.All(key => !string.IsNullOrWhiteSpace(RecordingCopy.Instance[key])),
            "English-only broadcasts have complete copy and no language-selection control");
        check(schema.Where(s => s.Name.StartsWith("recording", StringComparison.Ordinal)).All(s => s.Inputs.All(i => i.Name != "page"))
            && schema.Single(s => s.Name == "recordingMultikills").Inputs.All(i => i.Name != "samples"),
            "recording panels have no player pages or multikill side selector");
        var focus = panel with { References = new() { ["recordingCompare"] = new() { ["player"] = new("historical-player", "Old player", null, null, false) } } };
        check(RecordingOptions.From(settingsContext with { Panel = focus }, "round-compare").PlayerId == "historical-player"
            && RecordingOptions.From(settingsContext with { Panel = focus }, "team-economy").PlayerId is null,
            "host player references focus only their own broadcast panel section");
        return Task.CompletedTask;
    }
}
