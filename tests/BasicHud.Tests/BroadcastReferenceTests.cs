using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using BasicHud;
using BasicHud.Core;
using SOU.Live.Contracts;

static class BroadcastReferenceTests
{
    public static async Task Run(Action<bool, string> check)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var webroot = Path.Combine(root, "src/BasicHud/wwwroot");
        check(File.ReadAllText(Path.Combine(root, "LICENSES/valve-assets.json")) == File.ReadAllText(Path.Combine(webroot, "assets/valve-assets.json")), "packaged license provenance matches the actual original game asset inventory");
        using var optical = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs/cs2-icon-geometry.json")));
        var geometry = optical.RootElement.GetProperty("icons").EnumerateArray().ToArray();
        var expectedIcons = new[] { "weapons", "ui", "killfeed" }.SelectMany(folder => Directory.GetFiles(Path.Combine(webroot, "assets", folder), "*.svg").Select(file => folder + "/" + Path.GetFileNameWithoutExtension(file))).ToHashSet(StringComparer.Ordinal);
        check(geometry.Length == expectedIcons.Count && geometry.Select(icon => icon.GetProperty("key").GetString()!).ToHashSet(StringComparer.Ordinal).SetEquals(expectedIcons), "optical geometry covers every original game SVG without extra paths or duplicate identities");
        check(geometry.All(icon => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(webroot, "assets", icon.GetProperty("key").GetString()! + ".svg")))).ToLowerInvariant() == icon.GetProperty("sha256").GetString()), "optical measurements remain bound to the exact original SVG bytes");
        check(geometry.Where(icon => !icon.TryGetProperty("empty", out _)).All(icon => icon.GetProperty("x").GetDouble() >= 0 && icon.GetProperty("y").GetDouble() >= 0 && icon.GetProperty("width").GetDouble() > 0 && icon.GetProperty("height").GetDouble() > 0 && icon.GetProperty("x").GetDouble() + icon.GetProperty("width").GetDouble() <= 1.000001 && icon.GetProperty("y").GetDouble() + icon.GetProperty("height").GetDouble() <= 1.000001), "optical viewports contain the measured visible source geometry");
        var json = File.ReadAllText(Path.Combine(webroot, "assets/boltobserv/catalog.json"));
        var catalog = BoltRadarCatalog.Bind(JsonSerializer.Deserialize(json, RadarCatalogJson.Default.BoltRadarDocument));
        var dust = catalog.Find("de_dust2")!;
        check(dust is { Metadata.Resolution: 4.4, Metadata.Offset.X: 2470, Metadata.Offset.Y: 1255 }, "Boltobserv Dust2 uses the original calibration, not Valve coordinates with a replacement image");
        check(catalog.Find("workshop/123/de_dust2") == dust && catalog.Find("DE_DUST2") is null && catalog.Find("de_dust2_extra") is null, "Bolt map lookup accepts original workshop path normalization and rejects approximate identities");
        var center = BoltRadarCatalog.Project(dust, new(-217.2, 997.8, 0));
        check(Math.Abs(center.X - 50) < .0001 && Math.Abs(center.Y - 50) < .0001, "original Dust2 projection maps bottom-origin world coordinates to SVG top-origin coordinates");
        var nuke = catalog.Find("de_nuke")!;
        var heightFixture = new BoltRadarMap { Metadata = new() { ZRange = new() { Min = 0, Max = 100 }, Splits = [new() { Bounds = new() { Bottom = -200, Top = -100 }, ZRange = new() { Min = -190, Max = -110 } }] } };
        check(Math.Abs(BoltRadarCatalog.MarkerScale(heightFixture, 0) - .525) < .00001 && Math.Abs(BoltRadarCatalog.MarkerScale(heightFixture, 100) - .875) < .00001 && Math.Abs(BoltRadarCatalog.MarkerScale(heightFixture, 50) - .7) < .00001, "original Boltobserv height scaling keeps the marker and upright number at the same scale");
        check(Math.Abs(BoltRadarCatalog.MarkerScale(heightFixture, -150) - .7) < .00001 && BoltRadarCatalog.MarkerScale(heightFixture, double.NaN) == .7, "height scaling uses the matching strict split and safely handles missing height");
        var upper = BoltRadarCatalog.Project(nuke, new(0, 0, -482));
        var lower = BoltRadarCatalog.Project(nuke, new(0, 0, -483));
        check(Math.Abs(lower.Y - upper.Y - 46) < .0001 && BoltRadarCatalog.Project(nuke, new(0, 0, -2500)).Y == upper.Y, "Bolt Nuke projects each object into its composite image with original strict split bounds");
        var vertigo = catalog.Find("de_vertigo")!;
        var top = BoltRadarCatalog.Project(vertigo, new(0, 0, 11680));
        var bottom = BoltRadarCatalog.Project(vertigo, new(0, 0, 11679));
        check(Math.Abs(bottom.X - top.X - .2) < .0001 && Math.Abs(bottom.Y - top.Y - 42.6) < .0001, "Bolt Vertigo uses both original split offsets without switching the whole observed floor image");
        var broken = JsonSerializer.Deserialize(json, RadarCatalogJson.Default.BoltRadarDocument)!;
        broken.Maps[0].Image = "https://external.invalid/radar.png";
        check(ReferenceEquals(BoltRadarCatalog.Bind(broken), BoltRadarCatalog.Empty), "Bolt metadata cannot request external or traversing image paths");
        var now = DateTimeOffset.UtcNow;
        var player = new PlayerState("one", "Player", TeamSide.CounterTerrorists, null, 100) { ObserverSlot = 1, Position = new(-217.2, 997.8, 0), Forward = new(1, 0, 0), Flashed = 100 };
        var game = new GameState("de_dust2", MatchPhase.Live, 5, RoundPhase.Live, new("CT", 3), new("T", 1), player, [player]) { Countdown = new(CountdownPhase.Live, 90) };
        BroadcastContext Context(GameState value) => new(new(2, Guid.NewGuid(), 1, now, DataFreshness.Live, value), null, MetadataCatalog.Empty, now, _ => null) { BoltRadarCatalog = catalog, ResolveAsset = path => path };
        var plot = new RadarProjection().Project(Context(game));
        check(plot.IsBoltobserv && plot.Overview == dust.Image && plot.Markers.Single() is { Observed: true, Flashed: true, X: 50, Y: 50 } && plot.Buyzones is null, "real projection binds original Bolt image, observed/flash indicators and fixed coordinates without live buyzones");
        var freeze = game with { RoundPhase = RoundPhase.FreezeTime, Countdown = new(CountdownPhase.FreezeTime, 12) };
        check(new RadarProjection().Project(Context(freeze)).Buyzones == dust.Buyzones && new RadarProjection().Project(Context(game with { Phase = MatchPhase.Warmup })).Buyzones == dust.Buyzones, "buyzone overlay is visible only for actual freeze or warmup source states");
        var carrier = player with { Weapons = [new("bomb", "weapon_c4", WeaponCategory.Bomb, WeaponStatus.Holstered, null, null, null, null)] };
        check(new RadarProjection().Project(Context(game with { Players = [carrier], Bomb = new(BombStatus.Carried, "one", player.Position, null) })).Markers is [{ BombCarrier: true }], "carried bomb colors its actual carrier and does not draw a second dropped bomb");
        var smoke = new GrenadeState("smoke", GrenadeKind.Smoke, "one", player.Position, null, null, 1);
        var utility = new RadarProjection().Project(Context(game with { Grenades = [smoke, new("flight", GrenadeKind.HighExplosive, "one", player.Position, new(1, 1, 1), 1, null)] }));
        check(utility.Markers.Any(marker => marker.Kind == "smoke" && Math.Abs(marker.Diameter - 300 / 4.4 / 1024 * 100) < .0001) && utility.Markers.Any(marker => marker.Image == "assets/boltobserv/img/projectile-img-frag-CT.webp"), "Bolt smoke uses original world size and flying utility uses the correct original side artwork");
        var stages = new RadarProjection().Project(Context(game with
        {
            Grenades = [smoke with { Id = "zero", EffectTimeSeconds = 0 }, smoke with { Id = "unknown", EffectTimeSeconds = null }, smoke with { Id = "cloud", EffectTimeSeconds = 3 },
            new("flash-flight", GrenadeKind.Flashbang, "one", player.Position, null, .5, null), new("flash-detonated", GrenadeKind.Flashbang, "one", player.Position, null, 1.5, null)]
        }));
        check(stages.Markers.Single(marker => marker.Key == "grenade:zero").Kind == "grenade" && stages.Markers.Single(marker => marker.Key == "grenade:unknown").Kind == "grenade" && stages.Markers.Single(marker => marker.Key == "grenade:cloud").Kind == "smoke", "original smoke deployment requires a positive effecttime; zero and unknown still show a projectile");
        check(stages.Markers.Single(marker => marker.Key == "grenade:flash-flight").Kind == "grenade" && stages.Markers.Single(marker => marker.Key == "flash:flash-detonated") is { Kind: "flash", Image: null }, "original flash lifetime threshold separates flying flash from detonation even without effecttime");
        var pulse = new RadarProjection();
        var flashContext = Context(game with { Grenades = [new("pulse", GrenadeKind.Flashbang, "one", player.Position, null, 1.5, null)] });
        var initially = pulse.Project(flashContext).Markers.Single(marker => marker.Kind == "flash");
        var full = pulse.Project(flashContext with { Now = now.AddMilliseconds(100) }).Markers.Single(marker => marker.Kind == "flash");
        var fading = pulse.Project(flashContext with { Now = now.AddMilliseconds(1700) }).Markers.Single(marker => marker.Kind == "flash");
        check(initially.EffectOpacity == 0 && full.EffectOpacity == .8 && Math.Abs(fading.EffectOpacity - .4) < .0001 && pulse.Project(flashContext with { Now = now.AddMilliseconds(2200) }).Markers.All(marker => marker.Kind != "flash"), "original flash pulse opens at 100ms, fades after 1200ms and is removed at 2200ms using the existing presentation clock");
        check(pulse.Project(flashContext with { Now = now.AddSeconds(4) }).Markers.All(marker => marker.Kind != "flash") && pulse.Project(flashContext with { Snapshot = flashContext.Snapshot! with { SessionId = Guid.NewGuid() }, Now = now.AddSeconds(4) }).Markers.Any(marker => marker.Kind == "flash"), "replayed grenade snapshots never prolong or restart a flash, while a new source session clears its identity");
        var roundPulse = new RadarProjection();
        _ = roundPulse.Project(flashContext);
        check(roundPulse.Project(flashContext with { Snapshot = flashContext.Snapshot! with { Game = flashContext.Game! with { RoundNumber = 6 } }, Now = now.AddSeconds(4) }).Markers.Any(marker => marker.Kind == "flash"), "new round identity resets the bounded flash pulse state");
        check(new RadarProjection().Project(Context(game with { Bomb = new(BombStatus.Unknown, null, player.Position, null), RoundBomb = BombStatus.Exploded })).Markers.All(marker => marker.Key != "bomb"), "terminal round bomb state does not become a dropped bomb when the detailed bomb state is unknown");
        check(!MatchPresentation.ShowHistory(game) && !MatchPresentation.ShowHistory(game with { RoundPhase = RoundPhase.Over, Countdown = new(CountdownPhase.Over, 3) }), "live combat and ended round do not expose the round-history area");
        check(MatchPresentation.ShowHistory(freeze) && MatchPresentation.ShowHistory(game with { Countdown = new(CountdownPhase.Paused, null) }) && MatchPresentation.ShowHistory(game with { Countdown = new(CountdownPhase.TimeoutCounterTerrorists, 30) }), "freeze, actual pause and team timeout show results even when the source retains live round phase");
        check(!MatchPresentation.ShowHistory(freeze with { Bomb = new(BombStatus.Planted, null, null, 10) }) && !MatchPresentation.ShowHistory(freeze with { RoundBomb = BombStatus.Defusing }) && !MatchPresentation.ShowHistory(freeze with { Countdown = new(CountdownPhase.Defuse, 4) }), "actual bomb/defuse data excludes round history before any stale freeze state");
        check(MatchPresentation.RoundLabel(game, HudView.From(Context(game).Snapshot).Timer) == "ROUND 5" && MatchPresentation.RoundLabel(game with { RoundNumber = null }, new("ROUND TIME", "1:30", false)) == "", "central round label uses the source round once and omits an unknown round without inventing a denominator");
        check(MatchPresentation.RoundLabel(game, new("DEFUSING", "0:04", true)) == "DEFUSING", "bomb and defuse state replace the central normal round label");
        check(MatchPresentation.Caption(Context(game), "  Title  ") == "Title" && MatchPresentation.Caption(Context(game), null) == "" && DisplayOptions.Default.Caption == "", "explicit caption has priority and an absent competition never creates a Basic branding fallback");
        check(MatchPresentation.ReasonIcon(RoundWinReason.TimeExpired) == "time" && MatchPresentation.ReasonIcon(RoundWinReason.Unknown) == "unknown" && MatchPresentation.ReasonIcon(RoundWinReason.HostageRescued) == "hostage", "time, unknown and hostage outcomes are not rendered as elimination skulls");
        var resultGame = game with { RoundNumber = 20, RoundWins = Enumerable.Range(1, 22).Select(number => new RoundResult(number, TeamSide.CounterTerrorists, RoundWinReason.Elimination)).ToImmutableArray() };
        check(MatchPresentation.History(resultGame).Select(result => result.RoundNumber).SequenceEqual(Enumerable.Range(9, 12)), "recent history numbers twelve actual supplied results and excludes future entries");
        check(MatchPresentation.History(resultGame, true).Count == 20 && MatchPresentation.MapKey("evil/123/de_dust2") is null, "presentation retains all actual round results and arbitrary slash prefixes never impersonate a workshop map");
        var map = new ManagedMatchMap("inferno", "Inferno", "de_inferno", 2);
        check(!MatchPresentation.IsCurrentMap(game, map) && MatchPresentation.IsCurrentMap(game, map with { GameName = "de_dust2" }), "Dust2 never claims a current slot in an unrelated configured series");
        using var origins = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "LICENSES/boltobserv-origins.json")));
        var originalAssets = origins.RootElement.GetProperty("assets").EnumerateArray().ToArray();
        check(originalAssets.Length == 65 && originalAssets.All(asset =>
        {
            var bytes = File.ReadAllBytes(Path.Combine(webroot, asset.GetProperty("path").GetString()!.Replace('/', Path.DirectorySeparatorChar)));
            return bytes.LongLength == asset.GetProperty("bytes").GetInt64() && Convert.ToHexStringLower(SHA256.HashData(bytes)) == asset.GetProperty("sha256").GetString();
        }), "all 65 original Bolt images, metadata and utility files remain byte-identical to their recorded upstream source");
        using var http = new HttpClient(new BoltHandler(json));
        check((await RadarCatalogLoader.LoadBoltAsync(http, new Uri("http://127.0.0.1/hud-packages/sou-basic-wasm/0.4.2/"), CancellationToken.None)).Find("de_dust2") is not null, "actual source-generated bounded loader loads the package-local Bolt metadata without a JSON5 runtime");
    }
    sealed class BoltHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
    }
}
