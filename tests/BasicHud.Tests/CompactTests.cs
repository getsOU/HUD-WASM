using System.Net;
using System.Text;
using System.Text.Json;
using BasicHud;
using BasicHud.Core;
using SOU.Live.Contracts;

static class CompactTests
{
    public static async Task Run(Action<bool, string> check)
    {
        var document = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "radar-catalog.json")), RadarCatalogJson.Default.RadarCatalogDocument)!;
        var catalog = RadarCatalog.Bind(document);
        var dust = catalog.Select("de_dust2", null);
        check(dust is { Map.Name: "Dust II", Layer.Width: 1024, Layer.Height: 1024, Layer.PosX: -2476, Layer.PosY: 3239, Layer.Scale: 4.4 }, "shipped Dust2 overview uses actual fixed Valve calibration");
        check(catalog.Select("DE_DUST2", 0) is null && catalog.Select("de_unknown", 0) is null, "automatic map matching is exact and never guesses unknown geometry");
        var nuke = document.Maps.Single(map => map.Map == "de_nuke");
        var split = nuke.Layers.Select(layer => layer.MinZ).Where(z => z is > -10000).Max()!.Value;
        check(catalog.Select("de_nuke", split - .01)?.Layer.Id != catalog.Select("de_nuke", split)?.Layer.Id, "multilevel radar uses inclusive lower and exclusive upper height boundaries");
        check(catalog.Select("de_nuke", null) is { FloorKnown: false } && catalog.Select("de_dust2", null) is { FloorKnown: true }, "unknown observed height is explicit on multilevel maps only");
        var originalImage = document.Maps[0].Layers[0].Image;
        document.Maps[0].Layers[0].Image = "https://external.invalid/map.png";
        check(ReferenceEquals(RadarCatalog.Bind(document), RadarCatalog.Empty), "catalog cannot direct map images to an external origin");
        document.Maps[0].Layers[0].Image = originalImage;
        var nullImage = JsonSerializer.Deserialize("""{"schemaVersion":1,"maps":[{"map":"de_broken","layers":[{"id":"main","image":null,"width":1024,"height":1024,"scale":4}]}]}""", RadarCatalogJson.Default.RadarCatalogDocument);
        check(ReferenceEquals(RadarCatalog.Bind(nullImage), RadarCatalog.Empty), "malformed null image metadata degrades to an empty catalog instead of failing rendering");
        var player = new PlayerState("1", "Alice &amp; Bob &lt;3", TeamSide.CounterTerrorists, null, 100) { Position = new(-223.2, 986.2, 0), ObserverSlot = 1 };
        var now = DateTimeOffset.UtcNow;
        var game = new GameState("de_dust2", MatchPhase.Live, 1, RoundPhase.Live, new("COUNTER-TERRORISTS", 1), new("TERRORISTS", 0), player, [player]);
        BroadcastContext Context(GameState source) => new(new(2, Guid.NewGuid(), 1, now, DataFreshness.Live, source), null, MetadataCatalog.Empty, now, _ => null) { RadarCatalog = catalog, ResolveAsset = path => path };
        var plot = new RadarProjection().Project(Context(game));
        check(plot.Overview == "assets/radar/de_dust2.png" && Math.Abs(plot.Markers.Single().X - 50) < .0001 && Math.Abs(plot.Markers.Single().Y - 50) < .0001, "automatic Dust2 projection places real world coordinates on its fixed image");
        check(plot.Sites is [{ Label: "A" }, { Label: "B" }], "automatic radar exposes the actual supplied site labels from its catalog");
        var moved = new RadarProjection().Project(Context(game with { Players = [player with { Position = new(-1223.2, 986.2, 0) }] }));
        check(Math.Abs(moved.Markers.Single().X - (50 - 100000.0 / (1024 * 4.4))) < .0001, "map projection remains fixed when players move instead of resizing around them");
        check(new RadarProjection().Project(Context(game with { Players = [player with { Position = new(90000, 0, 0) }] })).Markers.Count == 0, "automatic radar also omits out-of-map positions rather than pinning them to an edge");
        var sourceConflict = new RadarProjection();
        var staleDead = game with { Players = [player with { Health = 0 }], Radar = new(Guid.NewGuid(), 1, 1, 50, now, [new("1", 1, player.Position!, new(1, 0, 0), 100)]) };
        var conflicted = Context(staleDead);
        var recovered = conflicted with { Snapshot = conflicted.Snapshot! with { Game = game, Revision = 2 } };
        check(!sourceConflict.Project(conflicted).Markers.Single().Dead && !sourceConflict.Project(recovered).Markers.Single().Dead, "fresh HLAE health overrides older GSI death without poisoning the round's death cache");
        var imageFailed = new RadarProjection().Project(Context(game) with { ResolveAsset = _ => null });
        check(imageFailed.Overview is null && imageFailed.Label.Contains("map unavailable", StringComparison.Ordinal), "a failed bundled overview returns to an honest fallback");
        var calibration = new HudPanelState(1, JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>("""{"radar":{"overview":"/cast-media/custom.png","mapName":"de_dust2","minX":"0","maxX":"1000","minY":"0","maxY":"1000"}}""")!);
        var custom = new RadarProjection().Project(Context(game with { Players = [player with { Position = new(500, 500, 0) }] }) with { Panel = calibration, ResolveMedia = path => path });
        check(custom.Overview == "/cast-media/custom.png" && custom.Label.StartsWith("Custom", StringComparison.Ordinal) && custom.Markers.Single().X == 50 && custom.Sites is null, "explicit valid custom overview overrides the bundled map without borrowing its site geometry");
        check(SourceText.Name(player.Name) == "Alice & Bob <3" && SourceText.Name("&amp;lt;script&amp;gt;") == "&lt;script&gt;", "game names decode exactly one entity layer and remain plain text");
        var view = HudView.From(Context(game).Snapshot);
        check(view.Ct.Name == "CT" && view.T.Name == "T" && view.Observed?.Name == "Alice & Bob <3", "generic source side names become concise labels without losing decoded player names");
        var match = new ManagedMatch("m", "Match", 1, new("a", "Alpha &amp;", "A", null, 0), new("b", "Bravo", "B", null, 0), new(TeamSide.CounterTerrorists, TeamRecognitionStatus.Unknown, null, 0, 0), new(TeamSide.Terrorists, TeamRecognitionStatus.Unknown, null, 0, 0));
        var metadata = MetadataCatalog.Bind(new BrowserMetadata { Match = new() { Left = new() { Id = "a", SteamIds = ["1"] }, Right = new() { Id = "b", SteamIds = ["2"] } }, Players = new() { ["1"] = new() { Name = "Profile &amp;" } } }, match);
        var profileContext = Context(game) with { Snapshot = Context(game).Snapshot! with { ManagedMatch = match }, Metadata = metadata };
        check(profileContext.Name(player) == "Profile &amp;", "library profile names remain unchanged rather than receiving game-source entity decoding");
        var rifle = new WeaponState("0", "weapon_ak47", WeaponCategory.Rifle, WeaponStatus.Active, null, 20, 30, 80);
        var grenade = new WeaponState("1", "weapon_flashbang", WeaponCategory.Grenade, WeaponStatus.Holstered, null, null, null, 2);
        check(EquipmentPresentation.Key("WEAPON_KNIFE_SURVIVAL_BOWIE") == "knife_survival_bowie" && EquipmentPresentation.Key("../secret") is null && EquipmentPresentation.Label("knife_survival_bowie") == "Knife", "equipment paths accept original safe names while knives keep a short display label");
        check(EquipmentPresentation.Ammo(rifle) == "20 / 80" && EquipmentPresentation.Ammo(grenade) == "", "only firearms display ammunition; utility never receives a fake ammo fraction");
        var utility = EquipmentPresentation.Utility(player with { Weapons = [grenade] });
        check(utility is [{ Name: "flashbang", Count: 2 }], "utility icons preserve the actual supplied stack count");
        check(EquipmentPresentation.Utility(player with { Weapons = [grenade with { AmmoReserve = null }] }) is [{ Count: null }], "missing utility quantities remain unknown instead of becoming one grenade");
        var requests = 0;
        Uri? requestedAddress = null;
        using var http = new HttpClient(new CatalogHandler((request, _) => { requests++; requestedAddress = request.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "radar-catalog.json")), Encoding.UTF8, "application/json") }); }));
        var loaded = await RadarCatalogLoader.LoadAsync(http, new Uri("http://127.0.0.1/hud-packages/sou-basic-wasm/0.3.0/"), CancellationToken.None);
        check(requests == 1 && requestedAddress?.AbsoluteUri == "http://127.0.0.1/hud-packages/sou-basic-wasm/0.3.0/assets/radar/catalog.json" && loaded.Select("de_dust2", null) is not null, "actual source-generated catalog loader makes one package-local request");
        using var failedHttp = new HttpClient(new CatalogHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));
        check(ReferenceEquals(await RadarCatalogLoader.LoadAsync(failedHttp, new Uri("http://127.0.0.1/"), CancellationToken.None), RadarCatalog.Empty), "an unavailable optional radar catalog does not fail the HUD");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        check(ReferenceEquals(await RadarCatalogLoader.LoadAsync(http, new Uri("http://127.0.0.1/"), cancelled.Token), RadarCatalog.Empty), "catalog loading observes disposal cancellation");
    }
    sealed class CatalogHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken); }
}
