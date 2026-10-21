using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Json;
using BasicHud;
using BasicHud.Core;
using SOU.Live.Contracts;

static class ShowcaseTests
{
    public static async Task Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var session = Guid.NewGuid();
        var connection = Guid.NewGuid();
        var match = new ManagedMatch("match", "Example", 3, new("A", "Alpha", "A", null, 0), new("B", "Bravo", "B", null, 0),
            new(TeamSide.CounterTerrorists, TeamRecognitionStatus.Manual, "A", 0, 0), new(TeamSide.Terrorists, TeamRecognitionStatus.Manual, "B", 0, 0));
        var player = new PlayerState("1", "Source player", TeamSide.CounterTerrorists, null, 100) { Position = new(10, 20, 0), Forward = new(1, 0, 0), Money = 1000, EquipmentValue = 2500, DefuseKit = true, Weapons = [] };
        var game = new GameState("de_example", MatchPhase.Live, 1, RoundPhase.Live, new(null, 0), new(null, 0), player, [player]);
        LiveSnapshot Snapshot(GameState source, long revision = 1) => new(2, session, revision, now, DataFreshness.Live, source) { ManagedMatch = match };
        BroadcastContext Context(GameState source, HudPanelState? panel = null, DateTimeOffset? at = null) => new(Snapshot(source), panel, MetadataCatalog.Empty, at ?? now,
            path => path == "/cast-media/map.png" ? "http://127.0.0.1/cast-media/map.png" : null);
        HudPanelState State(string json, long revision = 1) => new(revision, JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(json)!);

        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "hud.json")));
        var routes = manifest.RootElement.GetProperty("subviews").EnumerateArray().Select(item => item.GetProperty("route").GetString()).ToArray();
        check(routes.SequenceEqual(SceneSelection.Names.Skip(1).Select(name => "views/" + name)), "every presentation scene is declared in the shipped manifest");
        check(SceneSelection.Resolve("?preview=live", "statistics") == "statistics" && SceneSelection.Resolve("views/veto?x=1", "break") == "veto", "explicit subviews override the main-output scene selector");
        check(SceneSelection.Resolve("views/unknown", "prematch") == "live", "unknown scene names do not select arbitrary content");
        var schema = JsonSerializer.Deserialize<HudPanelSection[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "panel.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        check(schema.SelectMany(section => section.Inputs).Select(field => field.Type).Distinct().Count() == 9, "shipped showcase panel exercises all nine supported field types");
        check(LiveProjection.Economy([player]).Money == "$1,000" && LiveProjection.Economy([player, player with { Money = null }]).Money == "—", "economy totals require every summand and preserve missing data");
        check(LiveProjection.Economy([player, player with { Health = null }]).Alive.Contains("unknown", StringComparison.Ordinal), "alive totals identify unknown health instead of declaring death");
        var knownRoster = game with { AvailableSections = [GsiSection.AllPlayers] };
        check(LiveProjection.ForSide(knownRoster, TeamSide.CounterTerrorists).Money == "$1,000" && LiveProjection.ForSide(game, TeamSide.CounterTerrorists).Money == "—", "side totals require an explicitly available allplayers section");
        var unknownSide = knownRoster with { Players = [player, player with { SteamId = "2", Side = TeamSide.Unknown }] };
        check(LiveProjection.ForSide(unknownSide, TeamSide.CounterTerrorists).Money == "—" && LiveProjection.ForSide(unknownSide, TeamSide.CounterTerrorists).Alive.Contains("unknown", StringComparison.Ordinal), "globally unassigned players prevent apparently complete CT or T totals");
        check(LiveProjection.RoundMessage(game with { RoundPhase = RoundPhase.Over, RoundWinner = TeamSide.CounterTerrorists }).StartsWith("CT WIN", StringComparison.Ordinal), "round winner banners require explicit round winner and ended phase");
        check(LiveProjection.RoundMessage(game with { RoundWinner = TeamSide.CounterTerrorists }) == "", "a winner field does not trigger a banner during a live round");
        var bombPhaseOnly = game with { Bomb = null, RoundBomb = BombStatus.Planted, Countdown = new(CountdownPhase.Bomb, 32.2) };
        check(HudView.From(Snapshot(bombPhaseOnly)).Timer.Value == "0:33", "explicit planted state can use actual matching bomb-phase seconds without a bomb block");
        check(HudView.From(Snapshot(bombPhaseOnly with { Countdown = new(CountdownPhase.Live, 91) })).Timer.Value == "—", "planted state never borrows unrelated round countdown seconds");
        check(HudView.From(Snapshot(game with { RoundBomb = BombStatus.Defusing, Countdown = new(CountdownPhase.Defuse, 4.2) })).Timer.Value == "0:05", "explicit defusing state can use actual matching defuse-phase seconds");
        check(HudView.From(Snapshot(game with { RoundBomb = BombStatus.Planting, Countdown = new(CountdownPhase.Bomb, 35) })).Timer.Value == "—", "planting does not reinterpret a bomb-explosion countdown as plant duration");

        var metadata = new BrowserMetadata
        {
            Match = new() { Left = new() { Id = "B", SteamIds = ["2"] }, Right = new() { Id = "A", SteamIds = ["1"] } },
            Players = new() { ["1"] = new() { Name = "Managed", Avatar = "/cast-media/map.png", Camera = "https://external.invalid/camera" }, ["other"] = new() { Name = "Unrelated" } }
        };
        var catalog = MetadataCatalog.Bind(metadata, match);
        check(catalog.Matches(match) && catalog.Rosters["A"].SequenceEqual(["1"]) && catalog.Players["1"].Camera is null && !catalog.Players.ContainsKey("other"), "metadata binds by exact team IDs, filters membership and omits external cameras");
        check(!catalog.Matches(match with { TeamB = match.TeamB with { Id = "C" } }), "changed teams invalidate previously bound metadata even with the same match ID");
        check(MetadataCatalog.Bind(metadata, match with { TeamB = match.TeamB with { Id = "wrong" } }) == MetadataCatalog.Empty, "mismatched metadata never invents a prematch roster");
        var beforeGame = new BroadcastContext(Snapshot(game) with { Game = null }, null, catalog, now, path => path);
        check(beforeGame.ProfileImage("1") == "/cast-media/map.png" && beforeGame.ProfileImage("1", "/cast-media/featured.png") == "/cast-media/featured.png", "normal portrait mode preserves metadata images and explicit spotlight image priority");
        var agentsOnly = beforeGame with { Panel = State("{\"display\":{\"agentsOnly\":true}}") };
        check(agentsOnly.ProfileImage("1") is null && agentsOnly.ProfileImage("1", "/cast-media/featured.png") is null, "agents-only suppresses both manager portraits and explicit spotlight photos");
        check((agentsOnly with { Panel = State("{\"display\":{\"agentsOnly\":false}}") }).ProfileImage("1") == "/cast-media/map.png", "turning agents-only off restores the existing manager portrait without new metadata");
        check(agentsOnly.ResolveMedia("/cast-media/team-logo.png") == "/cast-media/team-logo.png", "agents-only does not suppress team or sponsor images");
        check(beforeGame.TeamPlayers(match.TeamA).Count == 1 && beforeGame.TeamPlayers(match.TeamA)[0].Health is null, "verified metadata supplies a real lineup before GSI without invented statistics");
        var duplicateMetadata = new BrowserMetadata { Match = new() { Left = new() { Id = "A", SteamIds = ["1"] }, Right = new() { Id = "B", SteamIds = ["1"] } }, Players = metadata.Players };
        check(MetadataCatalog.Bind(duplicateMetadata, match).Rosters.Values.All(ids => ids.Count == 0), "ambiguous membership is omitted rather than assigned to both teams");
        check(BroadcastContext.Coverage(new(3, 0, 0, 0, false)).Contains("partial", StringComparison.Ordinal), "partial recorded-statistics coverage stays explicit");
        var completed = match with { TeamA = match.TeamA with { SeriesWins = 2 }, Maps = [new("m1", "One", "one", 1) { Status = MapResultStatus.Completed, WinnerTeamId = "A" }, new("m2", "Two", "two", 2) { Status = MapResultStatus.Completed, WinnerTeamId = "A" }] };
        var completeContext = Context(game) with { Snapshot = Snapshot(game) with { ManagedMatch = completed } };
        check(completeContext.SeriesWinner == "Alpha" && (completeContext with { Snapshot = completeContext.Snapshot! with { ManagedMatch = completed with { Maps = [] } } }).SeriesWinner is null, "series winner requires supported rules and matching explicit completed-map wins");

        var calibration = State("{\"radar\":{\"overview\":\"/cast-media/map.png\",\"mapName\":\"de_example\",\"minX\":\"0\",\"maxX\":\"100\",\"minY\":\"0\",\"maxY\":\"100\"}}");
        var radar = new RadarProjection();
        var freshGame = game with { Radar = new(connection, 1, 1, 100, now, [new("1", 1, new(70, 80, 0), new(1, 0, 0), 100)]) };
        var fresh = radar.Project(Context(freshGame, calibration));
        check(fresh.Overview is not null && fresh.Label.EndsWith("HLAE", StringComparison.Ordinal) && fresh.Markers.Single().X == 70, "fresh matching HLAE coordinates use explicit local overview calibration");
        var stale = radar.Project(Context(freshGame, calibration, now.AddMilliseconds(751)));
        check(stale.Label.EndsWith("GSI", StringComparison.Ordinal) && stale.Markers.Single().X == 10, "HLAE expires after 750ms and falls back to real GSI positions");
        var wrongMap = radar.Project(Context(freshGame with { Map = "de_other" }, calibration));
        check(wrongMap.Overview is null && wrongMap.Label.StartsWith("Relative positions", StringComparison.Ordinal), "an overview is never shown for an unmatched map");
        var invalidBounds = State("{\"radar\":{\"overview\":\"/cast-media/map.png\",\"mapName\":\"de_example\",\"minX\":\"NaN\",\"maxX\":\"100\",\"minY\":\"0\",\"maxY\":\"100\"}}");
        check(radar.Project(Context(game, invalidBounds)).Overview is null, "non-finite overview boundaries cannot enable calibration");
        check(new RadarProjection().Project(Context(game with { Players = [player with { Position = new(500, 20, 0) }] }, calibration)).Markers.Count == 0, "out-of-bounds markers are omitted rather than clamped into fake corners");
        check(new RadarProjection().Project(Context(game with { Players = [player with { Position = new(double.NaN, 0, 0) }] })).Markers.Count == 0, "invalid source vectors never reach SVG coordinates");
        var flames = game with { Players = [], Grenades = [new("inferno", GrenadeKind.Inferno, "1", null, null, null, null) { Flames = [new("point", new(50, 50, 0))] }] };
        check(new RadarProjection().Project(Context(flames, calibration)).Markers.Single().Kind == "flame", "real inferno flame points render even without a central grenade position");
        var failedImage = Context(game, calibration) with { ResolveMedia = _ => null };
        var fallbackPlot = new RadarProjection().Project(failedImage);
        check(fallbackPlot.Overview is null && fallbackPlot.Label.StartsWith("Relative positions", StringComparison.Ordinal), "an unavailable overview recomputes honest relative geometry and labeling");
        var deathRadar = new RadarProjection();
        _ = deathRadar.Project(Context(game, calibration));
        var deadGame = game with { Players = [player with { Health = 0, Position = new(90, 90, 0) }] };
        check(deathRadar.Project(Context(deadGame, calibration)).Markers.Single().X == 10, "a death marker holds the last observed live position");
        check(deathRadar.Project(Context(deadGame with { RoundNumber = 2 }, calibration)).Markers.Single().X == 90, "a new round clears prior death-position state");
        var epochRadar = new RadarProjection();
        _ = epochRadar.Project(Context(freshGame, calibration));
        var backward = freshGame with { Radar = freshGame.Radar! with { Sequence = 2, GameTimeSeconds = 99 } };
        check(epochRadar.Project(Context(backward, calibration)).Label.EndsWith("GSI", StringComparison.Ordinal), "a backwards HLAE timeline is rejected until a new valid frame");
        var sequenceRadar = new RadarProjection();
        _ = sequenceRadar.Project(Context(freshGame with { Radar = freshGame.Radar! with { Sequence = 5 } }, calibration));
        check(sequenceRadar.Project(Context(freshGame with { Radar = freshGame.Radar! with { Sequence = 4, GameTimeSeconds = 101 } }, calibration)).Label.EndsWith("GSI", StringComparison.Ordinal), "backwards HLAE sequence is rejected even when game time moves forward");

        var kill = new KillFeedEntry(1, connection, 1, 100, null, new(1, "1", "Victim", TeamSide.CounterTerrorists), null, "world", WeaponKind.Unknown, false, false, false, false, false, false);
        var kills = new KillFeedProjection();
        check(kills.Project(Snapshot(game with { KillFeed = [kill] }), now).Single().Key.Contains(connection.ToString("N"), StringComparison.Ordinal), "kill identity includes connection and epoch rather than only event ID");
        check(kills.Project(Snapshot(game with { KillFeed = [kill] }), now.AddSeconds(9)).Count == 0, "replayed kill snapshots do not restart their visible lifetime");
        check(kills.Project(Snapshot(game with { Map = "new-map", KillFeed = [] }), now).Count == 0, "map reset removes previous kill state");

        var orderedKills = new KillFeedProjection();
        var events = Enumerable.Range(1, 7).Select(i => kill with { Id = i, GameTimeSeconds = 100 + i }).ToArray();
        check(orderedKills.Project(Snapshot(game with { KillFeed = [events[2], events[0], events[1]] }), now).Select(item => item.Event.Id).SequenceEqual(new long[] { 1, 2, 3 }), "killfeed places older kills above new kills, even when a frame arrives unordered");
        check(orderedKills.Project(Snapshot(game with { KillFeed = [.. events] }), now.AddMilliseconds(100)).Select(item => item.Event.Id).SequenceEqual(new long[] { 1, 2, 3, 4, 5, 6, 7 }), "all current kills remain visible beyond six with new kills appended below");
        check(orderedKills.Project(Snapshot(game with { KillFeed = [.. events] }), now.AddSeconds(9)).Count == 0, "repeated chronological frames do not renew kill lifetime");
        check(orderedKills.Project(Snapshot(game with { KillFeed = [kill with { Epoch = 2 }] }), now.AddSeconds(10)).Single().Event.Id == 1, "new epoch permits a restarted event ID without retaining old kills");

        var feed = new TestFeed();
        await using (var hud = new HudSession(feed))
        {
            hud.Start();
            await feed.Live.Writer.WriteAsync(Snapshot(game));
            await Until(() => hud.Snapshot is not null);
            var referenceState = State("{\"feature\":{\"player\":\"1\"}}", 7) with { References = new() { ["feature"] = new() { ["player"] = new("1", "Old name", null, "A", false) } } };
            await feed.Panel.Writer.WriteAsync(new("sou-basic-wasm", "0.2.0", referenceState));
            await Until(() => hud.Panel?.References["feature"]["player"].Name == "Old name");
            await feed.Panel.Writer.WriteAsync(new("sou-basic-wasm", "0.2.0", referenceState with { References = new() { ["feature"] = new() { ["player"] = new("1", "Updated name", null, "A", false) } } }));
            await Until(() => hud.Panel?.References["feature"]["player"].Name == "Updated name");
            check(hud.Panel?.Revision == 7, "same-revision reference refreshes are applied without replaying panel actions");
            var show = new HudPanelAction(Guid.NewGuid(), "production", "lowerThird", "show");
            await feed.Panel.Writer.WriteAsync(new("sou-basic-wasm", "0.2.0", Action: show));
            await Until(() => hud.LowerThirdVisible);
            await feed.Panel.Writer.WriteAsync(new("sou-basic-wasm", "0.2.0", Action: new(Guid.NewGuid(), "production", "lowerThird", "hide")));
            await Until(() => !hud.LowerThirdVisible);
            await feed.Panel.Writer.WriteAsync(new("sou-basic-wasm", "0.2.0", Action: show));
            await feed.Panel.Writer.WriteAsync(new("sou-basic-wasm", "0.2.0", State("{\"display\":{\"caption\":\"Barrier\"}}", 8)));
            await Until(() => hud.Options.Caption == "Barrier");
            check(!hud.LowerThirdVisible, "duplicate action IDs cannot replay a previously shown lower third");
            await feed.Panel.Writer.WriteAsync(new("sou-basic-wasm", "0.2.0", Action: new(Guid.NewGuid(), "production", "lowerThird", "show")));
            await Until(() => hud.LowerThirdVisible);
            await feed.Live.Writer.WriteAsync(Snapshot(game) with { SessionId = Guid.NewGuid() });
            await Until(() => !hud.LowerThirdVisible);
            check(!hud.LowerThirdVisible, "a new source session clears transient production state");
        }

        await TestMetadataPoller(check, Snapshot(game), metadata, match);
    }

    private static async Task TestMetadataPoller(Action<bool, string> check, LiveSnapshot snapshot, BrowserMetadata metadata, ManagedMatch match)
    {
        var result = new TaskCompletionSource<MetadataCatalog>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        using var http = new HttpClient(new ResponseHandler(request =>
        {
            check(request.RequestUri?.AbsolutePath == "/hud-browser/options" && request.RequestUri.Host == "127.0.0.1", "metadata polling requests only the package's fixed local endpoint");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json") };
        }));
        var poller = new MetadataPoller(http, new("http://127.0.0.1:5194/hud-packages/sou-basic-wasm/0.2.0/"));
        var work = poller.RunAsync(() => snapshot, (catalog, _) => result.TrySetResult(catalog), stop.Token);
        var catalog = await result.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await stop.CancelAsync(); await work;
        check(catalog.Matches(match), "actual source-generated metadata reader binds the matching team roster");

        var oversized = new TaskCompletionSource<MetadataCatalog>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var limitStop = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        using var largeHttp = new HttpClient(new ResponseHandler(_ => new(HttpStatusCode.OK) { Content = new StringContent(new string('x', 1024 * 1024 + 1)) }));
        var limitedWork = new MetadataPoller(largeHttp, new("http://127.0.0.1/hud-packages/x/0.2.0/")).RunAsync(() => snapshot, (next, _) => oversized.TrySetResult(next), limitStop.Token);
        check(await oversized.Task.WaitAsync(TimeSpan.FromSeconds(5)) == MetadataCatalog.Empty, "oversized optional metadata degrades to empty without breaking live streams");
        await limitStop.CancelAsync(); await limitedWork;

        var gate = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var changedResult = new TaskCompletionSource<MetadataCatalog>(TaskCreationOptions.RunContinuationsAsynchronously);
        var current = snapshot;
        using var raceStop = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        using var delayedHttp = new HttpClient(new AsyncResponseHandler(async token => { entered.TrySetResult(); return await gate.Task.WaitAsync(token); }));
        var raced = new MetadataPoller(delayedHttp, new("http://127.0.0.1/hud-packages/x/0.2.0/")).RunAsync(() => current, (next, _) => changedResult.TrySetResult(next), raceStop.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        current = current with { SessionId = Guid.NewGuid() };
        gate.TrySetResult(new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(metadata)) });
        check(await changedResult.Task.WaitAsync(TimeSpan.FromSeconds(3)) == MetadataCatalog.Empty, "metadata fetched for an earlier session is discarded after a context switch");
        await raceStop.CancelAsync(); await raced;

        var cancelEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancelStop = new CancellationTokenSource();
        using var blockedHttp = new HttpClient(new AsyncResponseHandler(async token =>
        {
            cancelEntered.TrySetResult(); await Task.Delay(Timeout.Infinite, token); return new(HttpStatusCode.OK);
        }));
        var blocked = new MetadataPoller(blockedHttp, new("http://127.0.0.1/hud-packages/x/0.2.0/")).RunAsync(() => snapshot, (_, _) => { }, cancelStop.Token);
        await cancelEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await cancelStop.CancelAsync();
        await blocked.WaitAsync(TimeSpan.FromSeconds(3));
        check(blocked.IsCompletedSuccessfully, "cancellation ends an in-flight metadata request and polling loop cleanly");
    }
    private static async Task Until(Func<bool> ready)
    {
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!ready()) await Task.Delay(10, stop.Token);
    }
    private sealed class ResponseHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
    private sealed class AsyncResponseHandler(Func<CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response(cancellationToken);
    }
}
