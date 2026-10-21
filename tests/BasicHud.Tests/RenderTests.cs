using BasicHud.Components;
using BasicHud.Core;
using BasicHud.Views;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SOU.Live.Contracts;

// Use the actual Razor sources and production _Imports, not a test copy of either component.
// RenderTree is the framework's renderer-test surface; no browser or third-party test runtime.
#pragma warning disable BL0006
static class RenderTests
{
    public static async Task Run(Action<bool, string> check)
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new TestRenderer(services, services.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var image = renderer.Attach(new ImageTile());
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/first.png", [nameof(ImageTile.Label)] = "Alpha" });
            check(renderer.HasElement(image, "img"), "actual ImageTile Razor renders its supplied image");
            var imageHandler = renderer.ErrorHandler(image);
            check(imageHandler != 0, "actual product ImageTile registers a native onerror callback, never a literal @onerror attribute");
            await renderer.DispatchEventAsync(imageHandler, null, EventArgs.Empty);
            check(!renderer.HasElement(image, "img") && renderer.HasElement(image, "span") && renderer.HasText(image, "A"), "dispatching the registered image error renders the neutral monogram");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/first.png", [nameof(ImageTile.Label)] = "Alpha" });
            check(!renderer.HasElement(image, "img"), "rerendering the failed source retains the fallback instead of requesting it again");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/second.png", [nameof(ImageTile.Label)] = "Bravo" });
            check(renderer.HasElement(image, "img") && renderer.ErrorHandler(image) != 0, "a changed image source resets the failure and registers its callback");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/live.png", [nameof(ImageTile.Label)] = "Alpha", [nameof(ImageTile.OmitFallback)] = true });
            await renderer.DispatchEventAsync(renderer.ErrorHandler(image), null, EventArgs.Empty);
            check(!renderer.HasElement(image, "img") && !renderer.HasElement(image, "span"), "failed compact live portraits omit their entire image box");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = null, [nameof(ImageTile.OmitFallback)] = true });
            check(!renderer.HasElement(image, "img") && !renderer.HasElement(image, "span"), "missing compact live images have no monogram placeholder");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = null, [nameof(ImageTile.Label)] = "Bravo", [nameof(ImageTile.OmitFallback)] = false });
            check(renderer.HasText(image, "B"), "presentation images retain neutral monograms independently of compact live omission");

            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = null, [nameof(ImageTile.FallbackSource)] = "assets/agents/ct-1.png", [nameof(ImageTile.Label)] = "Alpha", [nameof(ImageTile.OmitFallback)] = true });
            check(renderer.HasAttribute(image, "src", "assets/agents/ct-1.png") && renderer.HasAttribute(image, "alt", "Alpha (placeholder)"), "missing player images use the local agent and identify it as a placeholder");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/player.png" });
            check(renderer.HasAttribute(image, "src", "/cast-media/player.png"), "a supplied player portrait takes priority over the agent");
            await renderer.DispatchEventAsync(renderer.ErrorHandler(image), null, EventArgs.Empty);
            check(renderer.HasAttribute(image, "src", "assets/agents/ct-1.png"), "a failed supplied portrait falls back to its agent");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/player.png" });
            check(renderer.HasAttribute(image, "src", "assets/agents/ct-1.png"), "ordinary snapshots do not retry a failed supplied portrait");
            await renderer.DispatchEventAsync(renderer.ErrorHandler(image), null, EventArgs.Empty);
            check(!renderer.HasElement(image, "img"), "a failed agent terminates image retries and respects the final fallback policy");
            await renderer.Render(image, new() { [nameof(ImageTile.FallbackSource)] = "assets/agents/t-1.png" });
            check(renderer.HasAttribute(image, "src", "assets/agents/t-1.png"), "a side change resets the agent image even when the supplied portrait is still unavailable");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/new-player.png" });
            check(renderer.HasAttribute(image, "src", "/cast-media/new-player.png"), "a newly supplied player image replaces the fallback immediately");
            var weapon = renderer.Attach(new EquipmentIcon());
            await renderer.Render(weapon, new() { [nameof(EquipmentIcon.Name)] = "WEAPON_KNIFE_SURVIVAL_BOWIE" });
            check(renderer.HasAttribute(weapon, "src", "assets/weapons/knife_survival_bowie.svg") && renderer.ErrorHandler(weapon) != 0, "equipment Razor uses the original local silhouette with a real error callback");
            await renderer.DispatchEventAsync(renderer.ErrorHandler(weapon), null, EventArgs.Empty);
            check(!renderer.HasElement(weapon, "img") && !renderer.HasElement(weapon, "svg") && renderer.HasText(weapon, "Knife"), "unavailable equipment artwork retains its label without a fabricated replacement icon");

            var status = renderer.Attach(new StatusIcon());
            foreach (var name in new[] { "health", "armor", "helmet", "kit", "bomb", "dead", "headshot", "wallbang", "blind", "smoke", "noscope", "flashassist", "fire", "time", "hostage" })
            {
                await renderer.Render(status, new() { [nameof(StatusIcon.Name)] = name, [nameof(StatusIcon.Label)] = name });
                check(renderer.HasAttribute(status, "src", $"assets/{Cs2Icons.Status(name)}.svg") && !renderer.HasElement(status, "svg") && renderer.ErrorHandler(status) != 0, $"actual {name} status renders an original SVG image, not a hand-drawn path");
            }
            await renderer.DispatchEventAsync(renderer.ErrorHandler(status), null, EventArgs.Empty);
            check(!renderer.HasElement(status, "img") && !renderer.HasElement(status, "svg") && renderer.HasText(status, "hostage"), "failed status artwork retains text without substitute symbols");
            await renderer.Render(status, new() { [nameof(StatusIcon.Name)] = "health", [nameof(StatusIcon.Label)] = "Health" });
            check(renderer.HasElement(status, "img"), "a different original status asset remains available after an image error");
            await renderer.Render(status, new() { [nameof(StatusIcon.Name)] = "future-status", [nameof(StatusIcon.Label)] = "Future status" });
            check(!renderer.HasElement(status, "img") && !renderer.HasElement(status, "svg") && renderer.HasText(status, "Future status"), "unknown status uses its honest label without guessing artwork");

            var livePlayer = renderer.Attach(new LivePlayerCard());
            var agentPlayer = new PlayerState("agent", "Agent player", TeamSide.CounterTerrorists, null, 100) { ObserverSlot = 0 };
            await renderer.Render(livePlayer, new() { [nameof(LivePlayerCard.Player)] = agentPlayer, [nameof(LivePlayerCard.Context)] = BroadcastContext.Empty });
            check(renderer.HasDescendantAttribute(livePlayer, "src", "assets/agents/ct-0.png"), "actual live roster uses slot zero's CT agent when the manager supplies no portrait");
            var agentObserved = renderer.Attach(new ObservedPlayer());
            await renderer.Render(agentObserved, new() { [nameof(ObservedPlayer.Player)] = agentPlayer, [nameof(ObservedPlayer.Context)] = BroadcastContext.Empty });
            check(renderer.HasDescendantAttribute(agentObserved, "src", "assets/agents/ct-0.png"), "the observer and roster use the same slot agent");
            await renderer.Render(livePlayer, new() { [nameof(LivePlayerCard.Player)] = agentPlayer with { Side = TeamSide.Terrorists, Health = 0 }, [nameof(LivePlayerCard.Context)] = BroadcastContext.Empty });
            check(renderer.HasDescendantAttribute(livePlayer, "src", "assets/agents/t-0.png"), "a side swap updates the portrait even on a dead-player card");
            await renderer.Render(livePlayer, new() { [nameof(LivePlayerCard.Player)] = new PlayerState("1", "Rogue &lt;script&gt;", TeamSide.CounterTerrorists, null, null), [nameof(LivePlayerCard.Context)] = BroadcastContext.Empty });
            check(renderer.HasText(livePlayer, "Rogue <script>") && !renderer.HasMarkup(livePlayer, "Rogue"), "decoded source names remain actual Razor text, never markup");
            await renderer.Render(livePlayer, new() { [nameof(LivePlayerCard.Player)] = new PlayerState("1", "Dead player", TeamSide.CounterTerrorists, null, 0), [nameof(LivePlayerCard.Context)] = BroadcastContext.Empty });
            check((renderer.HasAttribute(livePlayer, "class", "card-death") || renderer.HasMarkup(livePlayer, "class=\"card-death\"")) && !renderer.HasAttribute(livePlayer, "class", "card-loadout") && !renderer.HasAttribute(livePlayer, "class", "card-health"), "dead reference card retains identity but replaces health and loadout with the death cue");
            check(renderer.HasDescendantAttribute(livePlayer, "data-icon", "ui/crosshair_circle") && renderer.HasDescendantAttribute(livePlayer, "data-icon", "ui/skull"), "reference card uses OpenHud kill and skull artwork for statistics");
            var spotlight = renderer.Attach(new SpotlightView());
            var profilePanel = new HudPanelState(1, new() { ["feature"] = new() { ["player"] = JsonSerializer.SerializeToElement("offline-player") } })
            { References = new() { ["feature"] = new() { ["player"] = new("offline-player", "Alice &amp; Bob", null, null, false) } } };
            await renderer.Render(spotlight, new() { [nameof(SpotlightView.Context)] = BroadcastContext.Empty with { Panel = profilePanel } });
            check(renderer.HasDescendantText(spotlight, "Alice &amp; Bob") && !renderer.HasDescendantText(spotlight, "Alice & Bob"), "actual offline spotlight preserves public profile/reference text without game-source decoding");

            var radar = renderer.Attach(new Radar());
            string? failedOverview = null;
            const string overview = "/cast-media/overview.png";
            await renderer.Render(radar, new()
            {
                [nameof(Radar.View)] = new RadarView("Configured overview", overview, []),
                [nameof(Radar.OverviewFailed)] = EventCallback.Factory.Create<string>(new object(), value => failedOverview = value),
            });
            check(renderer.HasElement(radar, "image"), "actual Radar Razor renders its configured SVG overview");
            var overviewHandler = renderer.ErrorHandler(radar);
            check(overviewHandler != 0, "actual product Radar registers a native SVG image onerror callback");
            await renderer.DispatchEventAsync(overviewHandler, null, EventArgs.Empty);
            check(failedOverview == overview, "dispatching the SVG image error invokes OverviewFailed with the actual image identity");
            await renderer.Render(radar, new() { [nameof(Radar.View)] = new RadarView("Relative positions", null, [new("one", "1", 50, 50, 0, TeamSide.CounterTerrorists, false, "player")]) });
            check(!renderer.HasElement(radar, "svg") && renderer.HasMarkup(radar, "Radar unavailable"), "an unavailable map renders one compact exceptional indicator and no substitute grid or relative markers");

            var referenceGame = new GameState("de_dust2", MatchPhase.Live, 5, RoundPhase.Live, new("CT", 3), new("T", 1), null, [])
            { Countdown = new(CountdownPhase.Live, 90), RoundWins = [new(1, TeamSide.CounterTerrorists, RoundWinReason.TimeExpired)] };
            var referenceMatch = new ManagedMatch("match", "Match", 3, new("a", "Alpha", "A", null, 1), new("b", "Bravo", "B", null, 0),
                new(TeamSide.CounterTerrorists, TeamRecognitionStatus.Recognized, "b", 0, 0), new(TeamSide.Terrorists, TeamRecognitionStatus.Recognized, "a", 0, 0))
            { Maps = [new("inferno", "Inferno", "de_inferno", 2)] };
            BroadcastContext reference = new(new(2, Guid.NewGuid(), 1, null, DataFreshness.Live, referenceGame) { ManagedMatch = referenceMatch }, null, MetadataCatalog.Empty, DateTimeOffset.UtcNow, _ => null);
            var scoreboard = renderer.Attach(new Scoreboard());
            await renderer.Render(scoreboard, new() { [nameof(Scoreboard.Context)] = reference, [nameof(Scoreboard.View)] = HudView.From(reference.Snapshot), [nameof(Scoreboard.Caption)] = "Chosen caption" });
            check(renderer.HasText(scoreboard, "Chosen caption") && renderer.HasText(scoreboard, "ROUND 5") && !renderer.HasText(scoreboard, "de_dust2") && !renderer.HasAttribute(scoreboard, "class", "match-meta"), "actual header renders one caption and central round without diagnostic map/phase/series strip");
            check(renderer.HasAttribute(scoreboard, "aria-label", "Bravo: 0 maps won in best of 3") && renderer.HasAttribute(scoreboard, "aria-label", "Alpha: 1 maps won in best of 3"), "actual side-aligned series marks follow swapped team identities instead of TeamA order");
            var series = renderer.Attach(new LiveMapSeries());
            await renderer.Render(series, new() { [nameof(LiveMapSeries.Context)] = reference });
            check(renderer.HasText(series, "Inferno") && !renderer.HasAttribute(series, "aria-current", "step") && !renderer.HasText(series, "Pending"), "actual compact map series does not mark unrelated Inferno current during Dust2 or expose technical status names");
            var history = renderer.Attach(new RoundHistory());
            await renderer.Render(history, new() { [nameof(RoundHistory.Context)] = reference });
            check(!renderer.HasElement(history, "section"), "actual history component has no residual DOM during live combat");
            var frozenReference = reference with { Snapshot = reference.Snapshot! with { Game = referenceGame with { RoundPhase = RoundPhase.FreezeTime, Countdown = new(CountdownPhase.FreezeTime, 12) } } };
            await renderer.Render(history, new() { [nameof(RoundHistory.Context)] = frozenReference });
            check(renderer.HasElement(history, "section") && renderer.HasText(history, "1") && renderer.HasDescendantAttribute(history, "alt", "Time expired"), "actual freeze history renders numbered results and the correct result icon description");
            await renderer.Render(history, new() { [nameof(RoundHistory.Context)] = reference, [nameof(RoundHistory.Always)] = true });
            check(renderer.HasElement(history, "section"), "statistics history explicitly stays available independently of live-phase gating");

            var held = new WeaponState("held", "weapon_ak47", WeaponCategory.Rifle, WeaponStatus.Active, null, 7, 30, 61);
            var weaponPlayer = new PlayerState("weapon-player", "Player", TeamSide.CounterTerrorists, null, 100) { Weapons = [held] };
            var observedPlayer = renderer.Attach(new ObservedPlayer());
            var inventory = renderer.Attach(new Inventory());
            foreach (var (description, source) in new[] {
                ("dead with stale active rifle", weaponPlayer with { Health = 0 }),
                ("alive with empty inventory", weaponPlayer with { Weapons = [] }),
                ("alive with unavailable inventory", weaponPlayer with { Weapons = null }) })
            {
                await renderer.Render(livePlayer, new() { [nameof(LivePlayerCard.Player)] = source, [nameof(LivePlayerCard.Context)] = BroadcastContext.Empty });
                await renderer.Render(observedPlayer, new() { [nameof(ObservedPlayer.Player)] = source, [nameof(ObservedPlayer.Context)] = BroadcastContext.Empty });
                check(!renderer.HasComponent(livePlayer, typeof(EquipmentIcon)) && !renderer.HasComponent(observedPlayer, typeof(EquipmentIcon)) && !renderer.HasAttribute(observedPlayer, "class", "ammo") && !renderer.HasDescendantText(observedPlayer, "Unknown weapon"), $"actual live/observer Razor omits active weapon, fallback and ammo for {description}");
                if (source.Health is <= 0 || PlayerPresentation.Weapons(source).Count == 0)
                {
                    await renderer.Render(inventory, new() { [nameof(Inventory.Player)] = source });
                    check(!renderer.HasElement(inventory, "div") && !renderer.HasComponent(inventory, typeof(EquipmentIcon)), $"actual detail inventory omits absent/stale weapon content for {description}");
                }
            }
            var holsteredPlayer = weaponPlayer with { Weapons = [held with { State = WeaponStatus.Holstered }] };
            await renderer.Render(livePlayer, new() { [nameof(LivePlayerCard.Player)] = holsteredPlayer, [nameof(LivePlayerCard.Context)] = BroadcastContext.Empty });
            await renderer.Render(observedPlayer, new() { [nameof(ObservedPlayer.Player)] = holsteredPlayer, [nameof(ObservedPlayer.Context)] = BroadcastContext.Empty });
            check(renderer.HasComponent(livePlayer, typeof(EquipmentIcon)) && !renderer.HasComponent(observedPlayer, typeof(EquipmentIcon)), "actual roster shows a carried holstered rifle while observer keeps its held-weapon area empty");
            var unknownWeaponPlayer = weaponPlayer with { Weapons = [held with { Name = null, Type = WeaponCategory.Unknown, Kind = WeaponKind.Unknown }] };
            await renderer.Render(livePlayer, new() { [nameof(LivePlayerCard.Player)] = unknownWeaponPlayer, [nameof(LivePlayerCard.Context)] = BroadcastContext.Empty });
            await renderer.Render(observedPlayer, new() { [nameof(ObservedPlayer.Player)] = unknownWeaponPlayer, [nameof(ObservedPlayer.Context)] = BroadcastContext.Empty });
            check(!renderer.HasComponent(livePlayer, typeof(EquipmentIcon)) && renderer.HasDescendantText(observedPlayer, "Unknown weapon") && !renderer.HasAttribute(observedPlayer, "class", "ammo"), "unknown category is not guessed as a roster firearm while observer preserves a supplied unknown held weapon");
            await renderer.Render(livePlayer, new() { [nameof(LivePlayerCard.Player)] = weaponPlayer with { Weapons = [held with { Name = "weapon_future_rifle" }] }, [nameof(LivePlayerCard.Context)] = BroadcastContext.Empty });
            check(renderer.HasComponent(livePlayer, typeof(EquipmentIcon)), "a classified roster rifle with unknown artwork retains its honest fallback");
            await renderer.Render(observedPlayer, new() { [nameof(ObservedPlayer.Player)] = weaponPlayer with { Weapons = [held with { State = WeaponStatus.Reloading }] }, [nameof(ObservedPlayer.Context)] = BroadcastContext.Empty });
            check(renderer.HasComponent(observedPlayer, typeof(EquipmentIcon)) && renderer.HasAttribute(observedPlayer, "aria-label", "7 / 61") && (renderer.HasAttribute(observedPlayer, "class", "reload") || renderer.HasMarkup(observedPlayer, "class=\"reload\"")), "actual observed reload keeps grouped source ammunition and a reload cue");
            check(!renderer.HasAttribute(observedPlayer, "class", "observed-weapon-name") && !renderer.HasDescendantText(observedPlayer, "AK-47"), "observer has no duplicate weapon label beside its accessible original icon");
            await renderer.Render(observedPlayer, new() { [nameof(ObservedPlayer.Player)] = weaponPlayer with { Health = 0 }, [nameof(ObservedPlayer.Context)] = BroadcastContext.Empty });
            check(!renderer.HasComponent(observedPlayer, typeof(EquipmentIcon)) && !renderer.HasAttribute(observedPlayer, "class", "ammo"), "transition from armed/reloading to dead clears the rendered current weapon and ammunition immediately");
            var utility = renderer.Attach(new UtilityIcons());
            await renderer.Render(utility, new() { [nameof(UtilityIcons.Player)] = weaponPlayer with { Weapons = [held with { Name = "weapon_flashbang", Type = WeaponCategory.Grenade, AmmoReserve = 2 }] } });
            check(renderer.AltTexts(utility).SequenceEqual(new[] { "Flashbang", "Flashbang" }) && !renderer.HasDescendantText(utility, "2"), "actual compact utility renders two original flashbang icons instead of a number");
            var feed = renderer.Attach(new Killfeed());
            var longName = "Complete player name without any abbreviation or ellipsis";
            var entry = new KillFeedEntry(1, Guid.NewGuid(), 1, 100, new(1, "killer", longName, TeamSide.CounterTerrorists), new(3, "victim", longName + " victim", TeamSide.Terrorists), new(2, "assist", longName + " assist", TeamSide.Terrorists), "weapon_ak47", WeaponKind.Ak47, true, true, true, true, true, true);
            await renderer.Render(feed, new() { [nameof(Killfeed.Entries)] = new[] { new KillView("key", entry) } });
            check(renderer.HasAttribute(feed, "class", "kill-assist t") && renderer.HasText(feed, longName + " assist") && renderer.HasText(feed, longName + " victim"), "actual killfeed preserves full participant names and the assister's own side");
            check(renderer.AltTexts(feed).SequenceEqual(new[] { "Attacker blind", "Flash assist", "AK-47", "Through smoke", "No scope", "Wallbang", "Headshot" }), "actual killfeed attaches blind and flash icons to participants and shot modifiers before the victim");
            await renderer.Render(feed, new() { [nameof(Killfeed.Entries)] = new[] { new KillView("key", entry with { Assister = null }) } });
            check(!renderer.AltTexts(feed).Contains("Flash assist"), "a missing assister does not leave an unattached flash-assist icon");
        });
        if (renderer.Exception is { } exception) throw new InvalidOperationException("Actual component render or event failed.", exception);
    }

    sealed class TestRenderer(IServiceProvider services, ILoggerFactory logger) : Renderer(services, logger)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public Exception? Exception { get; private set; }
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override void HandleException(Exception exception) => Exception = exception;
        public int Attach(IComponent component) => AssignRootComponentId(component);
        public Task Render(int id, Dictionary<string, object?> parameters) => RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters));
        public bool HasElement(int id, string name) => Frames(id).Any(frame => frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == name);
        public bool HasText(int id, string text) => Frames(id).Any(frame => frame.FrameType == RenderTreeFrameType.Text && frame.TextContent == text);
        public string[] AltTexts(int id) => Descendants(id).Where(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == "alt").Select(frame => frame.AttributeValue?.ToString() ?? "").ToArray();
        public bool HasAttribute(int id, string name, string value) => Frames(id).Any(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == name && Equals(frame.AttributeValue, value));
        public bool HasComponent(int id, Type type) => Frames(id).Any(frame => frame.FrameType == RenderTreeFrameType.Component && frame.ComponentType == type);
        public bool HasMarkup(int id, string text) => Frames(id).Any(frame => frame.FrameType == RenderTreeFrameType.Markup && frame.MarkupContent.Contains(text, StringComparison.Ordinal));
        public bool HasDescendantAttribute(int id, string name, string value) => Descendants(id).Any(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == name && Equals(frame.AttributeValue, value));
        public bool HasDescendantText(int id, string text) => Descendants(id).Any(frame => frame.FrameType == RenderTreeFrameType.Text && frame.TextContent == text);
        private IEnumerable<RenderTreeFrame> Descendants(int id)
        {
            foreach (var frame in Frames(id))
            {
                yield return frame;
                if (frame.FrameType == RenderTreeFrameType.Component)
                    foreach (var child in Descendants(frame.ComponentId)) yield return child;
            }
        }
        public ulong ErrorHandler(int id)
        {
            var attributes = Frames(id).Where(frame => frame.FrameType == RenderTreeFrameType.Attribute).ToArray();
            if (attributes.Any(frame => frame.AttributeName == "@onerror")) return 0;
            return attributes.SingleOrDefault(frame => frame.AttributeName == "onerror").AttributeEventHandlerId;
        }
        RenderTreeFrame[] Frames(int id)
        {
            var range = GetCurrentRenderTreeFrames(id);
            return range.Array.Take(range.Count).ToArray();
        }
    }
}
#pragma warning restore BL0006
