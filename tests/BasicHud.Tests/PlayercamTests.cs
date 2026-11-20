using System.Text.Json;
using BasicHud;
using BasicHud.Presentation;
using SOU.Hud.Sdk;
using SOU.Live.Contracts;

internal static class PlayercamTests
{
    public static void Run(Action<bool, string> check)
    {
        var capability = "#" + new string('a', 32) + ":" + new string('b', 64);
        var camera = "https://getsou.cam/playercams/view" + capability;
        foreach (var allowed in new[] { camera, "https://www.getsou.app/playercams/view" + capability, "https://getsou.cam:443/playercams/view" + capability.ToUpperInvariant() })
            check(PlayercamUrls.Viewer(allowed) == allowed, "playercam accepts only the approved HTTPS viewer shape: " + new Uri(allowed).Host);
        var localCamera = "/playercams/local/view/" + new string('c', 32) + capability;
        foreach (var allowed in new[] { localCamera, localCamera.ToUpperInvariant().Replace("/PLAYERCAMS/LOCAL/VIEW/", "/playercams/local/view/") })
            check(PlayercamUrls.Viewer(allowed) == allowed, "playercam preserves the exact root-relative local viewer address");
        foreach (var rejected in new[] { localCamera + "extra", localCamera + "\n", localCamera + " ", " " + localCamera,
            "/" + localCamera, localCamera.Replace("/view/", "/VIEW/"), localCamera.Replace("/view/", "/send/"),
            localCamera.Replace("/view/", "/view/../view/"), localCamera.Replace("/view/", "/view//"),
            localCamera.Replace("#", "/#"), localCamera.Replace("#", "?query=1#"), localCamera.Replace("#", "?#"),
            localCamera.Replace("/", "\\"), localCamera.Replace("cccc", "%63ccc"), localCamera.Replace("#", "%23"),
            localCamera.Replace(":", "%3A"), localCamera.Replace("#", "#extra"), localCamera.Replace("cccc", "gggg"),
            localCamera.Replace(new string('c', 32), new string('c', 31)), localCamera.Replace(new string('c', 32), new string('c', 33)),
            "http://127.0.0.1:5085" + localCamera, "https://127.0.0.1" + localCamera, "https://external.invalid" + localCamera,
            "https://getsou.cam" + localCamera, localCamera.Replace("/playercams/", "/hud-packages/playercams/") })
            check(PlayercamUrls.Viewer(rejected) is null, "playercam rejects altered, encoded, nonlocal or malformed local viewer addresses");
        foreach (var rejected in new string?[] { null, "", "javascript:alert(1)", "data:text/html,video", "http://getsou.cam/playercams/view" + capability,
            "https://external.invalid/playercams/view" + capability, "https://getsou.cam.evil.invalid/playercams/view" + capability,
            "https://user:password@getsou.cam/playercams/view" + capability, "https://getsou.cam:8443/playercams/view" + capability,
            "https://getsou.cam/playercams/send" + capability, "https://getsou.cam/playercams/view/" + capability,
            "https://getsou.cam/playercams/view?query=1" + capability, "https://getsou.cam/playercams/view#short", camera + "extra" })
            check(PlayercamUrls.Viewer(rejected) is null, "playercam rejects non-viewer, external or malformed addresses");
        var options = JsonSerializer.Deserialize("""{"players":{"p":{"camera":"CAMERA"}},"match":{"left":{"id":"A","steamIds":["p"]},"right":{"id":"B","steamIds":[]}}}""".Replace("CAMERA", camera), HudOptionsJson.Default.HudOptions)!;
        var match = new ManagedMatch("camera-match", "Camera test", 1, new("A", "Alpha", "A", null, 0), new("B", "Bravo", "B", null, 0),
            new(TeamSide.CounterTerrorists, TeamRecognitionStatus.Manual, "A", 0, 0), new(TeamSide.Terrorists, TeamRecognitionStatus.Manual, "B", 0, 0));
        var player = new PlayerState("p", "Player", TeamSide.CounterTerrorists, PlayerActivity.Playing, 100);
        var source = new LiveSnapshot(2, Guid.NewGuid(), 1, DateTimeOffset.UtcNow, DataFreshness.Live,
            new("de_mirage", MatchPhase.Live, 1, RoundPhase.Live, new("CT", 0), new("T", 0), player, [player]))
        { ManagedMatch = match };
        var metadata = MetadataCatalog.Bind(options.Metadata(), match);
        var context = new BroadcastContext(source, null, metadata, source.ReceivedAt!.Value, _ => null);
        check(options.Players!["p"].Camera == camera && metadata.Players["p"].Camera == camera && context.ProfileCamera("p") == camera,
            "generated options preserve an approved camera through metadata binding and observer lookup");
        var localOptions = options with { Players = options.Players!.SetItem("p", options.Players["p"] with { Camera = localCamera }) };
        var localContext = context with { Metadata = MetadataCatalog.Bind(localOptions.Metadata(), match) };
        check(localContext.ProfileCamera("p") == localCamera && localContext.PlayerCameras().Single().Viewer == localCamera,
            "local viewer address passes through metadata and the existing authorized warm camera pool unchanged");
        check((localContext with { SessionReady = false }).PlayerCameras().Count == 0
            && (localContext with { Metadata = MetadataCatalog.Bind((options with { Players = options.Players.SetItem("p", new()) }).Metadata(), match) }).PlayerCameras().Count == 0,
            "local camera receivers still require session readiness and an active camera grant");
        check(context.ProfileCamera("unlisted") is null && (context with { Metadata = MetadataCatalog.Empty }).ProfileCamera("p") is null
            && (context with { SessionReady = false }).ProfileCamera("p") is null
            && (context with { Snapshot = source with { ManagedMatch = match with { Id = "another-match" } } }).ProfileCamera("p") is null,
            "camera lookup requires active-session readiness and exact player/match membership");
        var panel = new HudPanelState(1, new() { ["display"] = new() { ["agentsOnly"] = JsonSerializer.SerializeToElement(true) } });
        check((context with { Panel = panel }).ProfileCamera("p") is null, "agents-only keeps the camera receiver disabled");
        var graph = new HudV3State().ApplyGraph(new HudV3Encoder().Encode(source));
        var changedOptions = options with { Players = options.Players.SetItem("p", options.Players["p"] with { Camera = "https://www.getsou.app/playercams/view" + capability }) };
        var next = context with { Metadata = MetadataCatalog.Bind(changedOptions.Metadata(), match) };
        check(HudRegionSelection.ResourcesChanged(HudArea.Observed, null, graph, context, next)
            && !HudRegionSelection.ResourcesChanged(HudArea.Player, graph.Roster!.Players[0], graph, context, next),
            "camera-only updates refresh the observer without changing playercard rendering dependencies");
        check(HudRegionSelection.ResourcesChanged(HudArea.Observed, null, graph, next, context with { Metadata = MetadataCatalog.Bind((options with { Players = options.Players.SetItem("p", new()) }).Metadata(), match) }),
            "removing a camera invalidates the observer even without a new game frame");
        var twoProfiles = options.Metadata();
        twoProfiles.Match!.Right!.SteamIds = ["q"];
        twoProfiles.Players!["q"] = new() { Camera = camera };
        var two = context with { Metadata = MetadataCatalog.Bind(twoProfiles, match) };
        check(two.PlayerCameras().Select(c => c.SteamId).SequenceEqual(["p", "q"]), "warm pool includes authorized unobserved players in stable roster order");
        check(HudRegionSelection.ResourcesChanged(HudArea.Observed, null, graph, context, two)
            && !HudRegionSelection.ResourcesChanged(HudArea.Player, graph.Roster!.Players[0], graph, context, two),
            "unobserved camera additions invalidate the pool without invalidating playercards");
        twoProfiles.Players["q"].Camera = null;
        var removedOther = context with { Metadata = MetadataCatalog.Bind(twoProfiles, match) };
        check(HudRegionSelection.ResourcesChanged(HudArea.Observed, null, graph, two, removedOther), "unobserved camera removals invalidate the pool");
        var ids = Enumerable.Range(0, 12).Select(i => "warm-" + i.ToString("D2")).ToArray();
        var many = new BrowserMetadata
        {
            Match = new() { Left = new() { Id = "A", SteamIds = [.. ids] }, Right = new() { Id = "B", SteamIds = [] } },
            Players = ids.ToDictionary(id => id, _ => new BrowserPlayer { Camera = camera })
        };
        var capped = context with { Metadata = MetadataCatalog.Bind(many, match) };
        check(capped.PlayerCameras().Count == 10 && capped.PlayerCameras().Select(c => c.SteamId).SequenceEqual(ids.Take(10)),
            "warm receivers are capped at ten independently of the observed player");
        check((capped with { SessionReady = false }).PlayerCameras().Count == 0 && (capped with { Panel = panel }).PlayerCameras().Count == 0
            && (capped with { Metadata = MetadataCatalog.Empty }).PlayerCameras().Count == 0,
            "session loss, image suppression and missing authorization clear the entire warm pool");
        check(CameraRoster.Players(two, false).Select(p => p.SteamId).SequenceEqual(["p"]) && CameraRoster.Players(two, true).Select(p => p.SteamId).SequenceEqual(["q"]), "roster views use Team A and Team B membership independently of observed player");
        var halftime = two with { Snapshot = source with { ManagedMatch = match with { Ct = match.Ct with { TeamId = "B" }, T = match.T with { TeamId = "A" } } } };
        check(CameraRoster.Team(halftime, false)?.Id == "A" && halftime.CurrentTeamSide("A") == TeamSide.Terrorists && CameraRoster.Players(halftime, false).Single().SteamId == "p", "halftime changes roster colors without swapping stable team membership");
        check(CameraRoster.Players(capped, false).Count == 12 && CameraRoster.VisibleCameras(capped, "roster-left").Count == 12, "actual rosters larger than five are retained even when camera receivers are capped at ten");
        check(CameraRoster.PoolEnabled(two with { Snapshot = source with { Game = source.Game! with { ObservedPlayer = null } } }, "roster-left"), "roster cameras do not require a currently observed player");
        var noObserver = two with { Snapshot = source with { SessionId = Guid.NewGuid(), Game = source.Game! with { ObservedPlayer = null } } };
        check(CameraRoster.PoolEnabled(noObserver, "live") && noObserver.PlayerCameras().SequenceEqual(two.PlayerCameras())
            && CameraRoster.VisibleCameras(noObserver, "live").Count == 0,
            "timeline reset and a missing observed player retain authorized warm cameras without displaying another player's feed");
        check(!CameraRoster.PoolEnabled(noObserver with { Panel = new(1, new() { ["display"] = new() { ["hideObservedPlayer"] = JsonSerializer.SerializeToElement(true) } }) }, "live")
            && (noObserver with { SessionReady = false }).PlayerCameras().Count == 0,
            "missing observer does not bypass explicit camera suppression or active-session authorization");
        check(!CameraRoster.PoolEnabled(two, "statistics") && !CameraRoster.PoolEnabled(two with { Panel = new(1, new() { ["display"] = new() { ["hideRosters"] = JsonSerializer.SerializeToElement(true) } }) }, "roster-left"), "unrelated scenes and hidden rosters release the shared pool");
        var unchangedOrder = new BrowserMetadata { Match = new() { Left = new() { Id = "A", SteamIds = ["p", "q"] }, Right = new() { Id = "B", SteamIds = ["r"] } }, Players = new() { ["p"] = new() { Camera = camera }, ["q"] = new() { Camera = camera }, ["r"] = new() { Camera = camera } } };
        var beforeTransfer = context with { Metadata = MetadataCatalog.Bind(unchangedOrder, match) };
        unchangedOrder.Match.Left.SteamIds = ["p"]; unchangedOrder.Match.Right.SteamIds = ["q", "r"];
        var afterTransfer = context with { Metadata = MetadataCatalog.Bind(unchangedOrder, match) };
        check(beforeTransfer.PlayerCameras().SequenceEqual(afterTransfer.PlayerCameras()) && HudRegionSelection.ResourcesChanged(HudArea.Cameras, null, graph, beforeTransfer, afterTransfer)
            && CameraRoster.VisibleCameras(beforeTransfer, "roster-left").Contains("q") && !CameraRoster.VisibleCameras(afterTransfer, "roster-left").Contains("q"), "team transfers invalidate camera visibility even when the combined warm source order stays identical");
        var emptyRoster = context with { Metadata = MetadataCatalog.Bind(new BrowserMetadata { Match = new() { Left = new() { Id = "A", SteamIds = [] }, Right = new() { Id = "B", SteamIds = [] } } }, match) };
        check(CameraRoster.Players(emptyRoster, false).Count == 0, "an explicitly empty verified roster does not repopulate from live side players");
        check(new[] { 2, 3, 5 }.Select(CameraRoster.UpperCount).SequenceEqual([1, 2, 3]), "two roster rows put the odd player in the upper row: 1+1, 2+1, 3+2");
        var swappedSelection = options.Metadata();
        swappedSelection.Match = new() { Left = new() { Id = "B", SteamIds = ["q"] }, Right = new() { Id = "A", SteamIds = ["p"] } };
        var swappedContext = two with { Metadata = MetadataCatalog.Bind(swappedSelection, match) };
        check(CameraRoster.Team(swappedContext, false)?.Id == "B" && CameraRoster.Team(swappedContext, true)?.Id == "A"
            && HudRegionSelection.ResourcesChanged(HudArea.Cameras, null, graph, two, swappedContext), "roster left/right follow the selected sOU match teams even when their order differs from Team A/B");
        var beforeGameCameras = context with { Snapshot = source with { Game = null } };
        check(beforeGameCameras.PlayerCameras().Count == 0 && beforeGameCameras.PlayerCameras(requireGame: false).Count == 1
            && (beforeGameCameras with { SessionReady = false }).PlayerCameras(requireGame: false).Count == 0, "authorized roster cameras work without GSI while session readiness and live observer gating remain enforced");
    }
}
