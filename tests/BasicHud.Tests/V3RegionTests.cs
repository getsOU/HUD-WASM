using System.Collections.Immutable;
using System.Text.Json;
using BasicHud;
using BasicHud.Presentation;
using SOU.Hud.Sdk;
using SOU.Live.Contracts;

internal static class V3RegionTests
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var encoder = new HudV3Encoder();
        var reducer = new HudV3State();
        var p1 = new PlayerState("same", "First", TeamSide.CounterTerrorists, PlayerActivity.Playing, 100)
        {
            ObserverSlot = 1,
            Position = new(1, 2, 3),
            Weapons = [new("primary", "weapon_m4a1", WeaponCategory.Rifle, WeaponStatus.Active, null, 30, 30, 90)]
        };
        var p2 = p1 with { SteamId = "other", Name = "Second", ObserverSlot = 2 };
        var source = new LiveSnapshot(2, Guid.NewGuid(), 1, now, DataFreshness.Live,
            new("de_mirage", MatchPhase.Live, 1, RoundPhase.Live, new("CT", 0), new("T", 0), p1, [p1, p2]) { Countdown = new(CountdownPhase.Live, 90) });
        HudSessionSnapshot<HudOptions> Frame(LiveSnapshot snapshot) => new(true, HudConnectionStatus.Connected,
            reducer.ApplyGraph(encoder.Encode(snapshot)), null, null, new(), null, snapshot.Revision);
        var first = Frame(source);
        var keys = first.Game!.Roster!.Players;
        bool Same(HudSessionSnapshot<HudOptions> a, HudSessionSnapshot<HudOptions> b, HudArea area, string? key = null) =>
            HudRegionSelection.Comparer.Equals(HudRegionSelection.Select(a, area, key), HudRegionSelection.Select(b, area, key));

        var moved = p1 with { Position = new(10, 20, 3) };
        var pose = Frame(source with { Revision = 2, Game = source.Game! with { Players = [moved, p2], ObservedPlayer = moved } });
        check(!Same(first, pose, HudArea.Radar) && Same(first, pose, HudArea.Scoreboard) && Same(first, pose, HudArea.Player, keys[0])
            && Same(first, pose, HudArea.Player, keys[1]) && Same(first, pose, HudArea.Observed), "v3 pose updates select radar alone, leaving scoreboard, observer and both cards unchanged");
        check(!pose.Game!.IsCompatibilitySnapshotCreated, "v3 selectors do not materialize the compatibility snapshot while comparing dependencies");
        check(Same(first, pose, HudArea.Mvp), "MVP selection ignores pose-only updates");

        var shot = moved with { Weapons = [moved.Weapons!.Value[0] with { AmmoClip = 29 }] };
        var ammo = Frame(source with { Revision = 3, Game = source.Game with { Players = [shot, p2], ObservedPlayer = shot } });
        check(!Same(pose, ammo, HudArea.Player, keys[0]) && !Same(pose, ammo, HudArea.Observed)
            && Same(pose, ammo, HudArea.Player, keys[1]) && Same(pose, ammo, HudArea.Scoreboard) && Same(pose, ammo, HudArea.Radar)
            && Same(pose, ammo, HudArea.CtRoster), "v3 ammunition selects only the affected card and observer, without refreshing the roster container");

        var observed = Frame(source with { Revision = 4, Game = source.Game with { Players = [shot, p2], ObservedPlayer = p2 } });
        check(!Same(ammo, observed, HudArea.Player, keys[0]) && !Same(ammo, observed, HudArea.Player, keys[1]) && !Same(ammo, observed, HudArea.Observed),
            "observer switches invalidate the old/new card borders and the observed-player region");
        var duplicate = Frame(source with { Revision = 5, Game = source.Game with { Players = [p1, p2 with { SteamId = p1.SteamId }] } });
        var duplicateKeys = duplicate.Game!.Roster!.Players;
        var presentation = new HudRegionPresentation(duplicate.Game, new(duplicate.Snapshot, null, MetadataCatalog.Empty, now, _ => null), DisplayOptions.Default);
        check(presentation.Player(duplicateKeys[0])?.Name == "First" && presentation.Player(duplicateKeys[1])?.Name == "Second",
            "opaque roster keys preserve distinct players even when Steam IDs are duplicated");
        var lost = observed with { Ready = false, Game = null, Connection = HudConnectionStatus.Reconnecting };
        check(Enum.GetValues<HudArea>().All(area => !Same(observed, lost, area, keys[0])), "disconnect invalidates every selected area before accepting a new baseline");

        var context = new BroadcastContext(source, null, MetadataCatalog.Empty, now.AddSeconds(2.2), _ => null);
        check(HudClockPresentation.View(context).Timer.Value == "1:28", "regional score clock advances from the original source anchor using synchronized time");
        check(HudClockPresentation.View(context with { Snapshot = source with { Game = source.Game with { Countdown = new(CountdownPhase.Paused, 90) } } }).Timer.Value == "1:30",
            "paused countdowns remain fixed while the shared regional clock ticks");
        check(HudClockPresentation.View(context with { Snapshot = source with { ReceivedAt = null } }).Timer.Value == "—", "missing timer anchors remain unavailable rather than inventing elapsed time");

        var profiles = JsonSerializer.Deserialize("""{"caption":"First","players":{"same":{"name":"Profile name","avatar":"/cast-media/player.png","camera":"ignored"}},"match":{"left":{"id":"A","steamIds":["same"]},"right":{"id":"B","steamIds":["other"]}}}""", HudOptionsJson.Default.HudOptions)!;
        var renamed = JsonSerializer.Deserialize("""{"caption":"Second","players":{"same":{"name":"Profile name","avatar":"/cast-media/player.png"}},"match":{"left":{"id":"A","steamIds":["same"]},"right":{"id":"B","steamIds":["other"]}}}""", HudOptionsJson.Default.HudOptions)!;
        var match = new ManagedMatch("match", "Example", 1, new("A", "Alpha", "A", null, 0), new("B", "Bravo", "B", null, 0),
            new(TeamSide.CounterTerrorists, TeamRecognitionStatus.Manual, "A", 0, 0), new(TeamSide.Terrorists, TeamRecognitionStatus.Manual, "B", 0, 0));
        var catalog = MetadataCatalog.Bind(profiles.Metadata(), match);
        check(catalog.Matches(match) && catalog.Players["same"].Name == "Profile name" && catalog.Players["same"].Avatar == "/cast-media/player.png" && catalog.Players["same"].Camera is null,
            "generated v3 options bind immutable profile metadata to the active teams without retaining camera input");
        check(Same(first with { Options = profiles }, first with { Options = renamed }, HudArea.Resources)
            && !Same(first with { Options = profiles }, first with { Options = renamed }, HudArea.Scoreboard),
            "caption-only options changes update the header without rebinding identical player profiles");
        check(MetadataCatalog.Bind(profiles.Metadata(), match with { TeamB = match.TeamB with { Id = "different" } }) == MetadataCatalog.Empty,
            "v3 profile metadata for another team assignment is discarded");
        var profileFrame = Frame(source with { Revision = 6, ManagedMatch = match });
        var resources = context with { Metadata = catalog };
        var otherProfiles = profiles with { Players = profiles.Players!.Add("other", new("New other portrait", "/cast-media/other.png")) };
        var nextResources = resources with { Metadata = MetadataCatalog.Bind(otherProfiles.Metadata(), match) };
        check(!HudRegionSelection.ResourcesChanged(HudArea.Player, keys[0], profileFrame.Game, resources, nextResources)
            && HudRegionSelection.ResourcesChanged(HudArea.Player, keys[1], profileFrame.Game, resources, nextResources),
            "a profile-only options update invalidates its own card and preserves an unrelated player's rendering");
        check(!HudRegionSelection.ResourcesChanged(HudArea.Scoreboard, null, profileFrame.Game, resources, nextResources)
            && !HudRegionSelection.ResourcesChanged(HudArea.Radar, null, profileFrame.Game, resources, nextResources),
            "profile changes do not invalidate scoreboard or radar resources");
        var stale = Frame(source with { Revision = 7, ManagedMatch = match, Freshness = DataFreshness.Stale });
        check(!Same(profileFrame, stale, HudArea.Scoreboard), "freshness transitions invalidate the score clock so stale countdowns stop advancing");
        var bomb = Frame(source with { Revision = 8, ManagedMatch = match, Freshness = DataFreshness.Stale, Game = source.Game with { Bomb = new(BombStatus.Planted, null, null, 30) } });
        check(!Same(stale, bomb, HudArea.History), "bomb state changes invalidate round-history visibility independently of countdown phase");
        var mvpPlayer = shot with { Stats = new(2, 0, 0, 1, 0), RoundKills = 2, RoundDamage = 200 };
        var mvpFrame = Frame(source with { Revision = 9, Game = source.Game with { Players = [mvpPlayer, p2] } });
        check(!Same(ammo, mvpFrame, HudArea.Mvp) && Same(ammo, pose, HudArea.Mvp),
            "MVP selection reacts to statistics while ignoring ammunition updates");
        var kill = new KillFeedEntry(1, Guid.NewGuid(), 1, 90, new(1, "same", "Event first", TeamSide.CounterTerrorists),
            new(2, "other", "Event second", TeamSide.CounterTerrorists), null, "weapon_ak47", WeaponKind.Ak47, false, false, false, false, false, false);
        var killFrame = Frame(source with { Revision = 10, ManagedMatch = match, Game = source.Game with { KillFeed = [kill] } });
        check(HudRegionSelection.ResourcesChanged(HudArea.Killfeed, null, killFrame.Game, resources, nextResources),
            "a participant profile-name change refreshes an existing killfeed event");
        var portraitOnly = profiles with { Players = profiles.Players!.SetItem("same", new("Profile name", "/cast-media/new-portrait.png")) };
        var portraitResources = resources with { Metadata = MetadataCatalog.Bind(portraitOnly.Metadata(), match) };
        check(!HudRegionSelection.ResourcesChanged(HudArea.Killfeed, null, killFrame.Game, resources, portraitResources)
            && !HudRegionSelection.ResourcesChanged(HudArea.Killfeed, null, profileFrame.Game, resources, nextResources),
            "portrait-only profile changes and profiles without kill participants do not refresh the killfeed");
        var killMoved = Frame(source with { Revision = 11, ManagedMatch = match, Game = source.Game with { KillFeed = [kill], Players = [moved, p2] } });
        var killRebound = Frame(source with { Revision = 12, ManagedMatch = match with { Id = "new-match" }, Game = source.Game with { KillFeed = [kill], Players = [moved, p2] } });
        check(Same(killFrame, killMoved, HudArea.Killfeed) && !Same(killMoved, killRebound, HudArea.Killfeed)
            && !killRebound.Game!.IsCompatibilitySnapshotCreated,
            "killfeed selects match binding changes while ignoring movement and avoiding compatibility snapshot materialization");
        var roundPlayer = p1 with { RoundKills = 2 };
        var roundGame = source.Game with { Players = [roundPlayer, p2], ObservedPlayer = roundPlayer };
        var roundLive = Frame(source with { Revision = 13, Game = roundGame });
        var roundFreeze = Frame(source with { Revision = 14, Game = roundGame with { RoundPhase = RoundPhase.FreezeTime } });
        check(!Same(roundLive, roundFreeze, HudArea.Player, keys[0]) && Same(roundLive, roundFreeze, HudArea.Observed),
            "freeze refreshes playercard round-kill visibility without adding an observer dependency");
        var waitingSource = source with { Revision = 15, Game = null, Freshness = DataFreshness.Waiting, ManagedMatch = match with { StartsAt = now.AddMinutes(5) } };
        var waitingStart = Frame(waitingSource);
        var waitingDelay = Frame(waitingSource with { Revision = 16, ManagedMatch = waitingSource.ManagedMatch! with { DelayedStartsAt = now.AddMinutes(15) } });
        check(waitingDelay.Game!.ManagedMatch?.DelayedStartsAt == now.AddMinutes(15) && !Same(waitingStart, waitingDelay, HudArea.Presentation),
            "v3 SDK carries a delayed start before GSI and updates the presentation region when only that field changes");
        var removedDelay = Frame(waitingSource with { Revision = 17 });
        check(removedDelay.Game!.ManagedMatch?.DelayedStartsAt is null && !Same(waitingDelay, removedDelay, HudArea.Presentation),
            "removing a delayed start propagates through the v3 graph and invalidates the start display");
    }
}
