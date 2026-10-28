using System.Globalization;
using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public sealed record RadarMarker(string Key, string Label, double X, double Y, double? Angle, TeamSide? Side, bool Dead, string Kind, string? Elevation = null)
{
    public string MotionKey { get; init; } = "";
    public bool Observed { get; init; }
    public bool BombCarrier { get; init; }
    public bool Flashed { get; init; }
    public string? Image { get; init; }
    public double Diameter { get; init; } = 3.92;
    public double Scale { get; init; } = .7;
    public double EffectOpacity { get; init; } = 1;
}
public sealed record RadarView(string Label, string? Overview, IReadOnlyList<RadarMarker> Markers, IReadOnlyList<RadarSite>? Sites = null)
{
    public string? Buyzones { get; init; }
    public bool IsBoltobserv { get; init; }
}
public sealed class RadarProjection
{
    private string resetKey = "";
    private string timeline = "";
    private double lastGameTime = double.MinValue;
    private long lastSequence = long.MinValue;
    private long motionGeneration;
    private bool? previousFresh;
    private RoundPhase? previousPhase;
    private string lastFrame = "", blockedFrame = "";
    private readonly Dictionary<string, Vector3State> positions = [];
    private readonly Dictionary<string, Vector3State> deadPositions = [];
    private readonly Dictionary<string, FlashPulse> flashes = [];
    private sealed record FlashPulse(Vector3State Position, TeamSide? Side, DateTimeOffset StartedAt);

    public RadarView Project(BroadcastContext context)
    {
        var game = context.Game;
        var nextReset = $"{context.Snapshot?.SessionId}:{game?.Map}:{game?.RoundNumber}";
        if (resetKey != nextReset || game?.RoundPhase == RoundPhase.FreezeTime && previousPhase != RoundPhase.FreezeTime)
        { positions.Clear(); deadPositions.Clear(); flashes.Clear(); timeline = ""; lastGameTime = double.MinValue; lastSequence = long.MinValue; resetKey = nextReset; blockedFrame = lastFrame; motionGeneration++; }
        previousPhase = game?.RoundPhase;
        var radar = game?.Radar;
        var frame = $"{radar?.ConnectionId}:{radar?.Epoch}:{radar?.Sequence}";
        var fresh = radar is not null && radar.ConnectionId != Guid.Empty && radar.Epoch >= 0 && radar.Sequence >= 0 && double.IsFinite(radar.GameTimeSeconds)
            && frame != blockedFrame && context.Now >= radar.ReceivedAt && context.Now - radar.ReceivedAt < TimeSpan.FromMilliseconds(750);
        if (fresh)
        {
            var nextTimeline = $"{radar!.ConnectionId}:{radar.Epoch}";
            if (nextTimeline != timeline) { positions.Clear(); deadPositions.Clear(); timeline = nextTimeline; lastGameTime = double.MinValue; lastSequence = long.MinValue; }
            if (radar.GameTimeSeconds < lastGameTime || radar.Sequence < lastSequence) fresh = false;
            else { lastGameTime = radar.GameTimeSeconds; lastSequence = radar.Sequence; lastFrame = frame; }
        }
        if (previousFresh != fresh)
        {
            positions.Clear(); deadPositions.Clear(); motionGeneration++;
            previousFresh = fresh;
        }
        var raw = new List<(string Key, string Label, Vector3State Position, Vector3State? Forward, TeamSide? Side, bool Dead, string Kind)>();
        var activeIds = context.Players.Take(64).Where(player => player.SteamId is not null).Select(player => player.SteamId!).ToHashSet(StringComparer.Ordinal);
        foreach (var old in positions.Keys.Where(id => !activeIds.Contains(id)).ToArray()) positions.Remove(old);
        foreach (var old in deadPositions.Keys.Where(id => !activeIds.Contains(id)).ToArray()) deadPositions.Remove(old);
        foreach (var player in context.Players.Take(64))
        {
            var hlae = fresh && player.SteamId is { } id && !radar!.Players.IsDefault ? radar.Players.FirstOrDefault(item => item.SteamId == id) : null;
            // Like NYXIA: a fresh HLAE frame is one coherent source. Missing pawns do not jump to older GSI poses.
            var position = fresh ? hlae?.Position : player.Position;
            var forward = fresh ? hlae?.Forward : player.Forward;
            var dead = (hlae?.Health ?? player.Health) is <= 0;
            var key = player.SteamId;
            if (key is null || !Finite(position)) continue;
            if (dead)
            {
                if (!deadPositions.TryGetValue(key, out var death)) { death = positions.GetValueOrDefault(key) ?? position!; deadPositions[key] = death; }
                position = death;
            }
            else
            {
                if (game?.Phase == MatchPhase.Warmup || game?.Mode == GameMode.Deathmatch) deadPositions.Remove(key);
                if (deadPositions.TryGetValue(key, out var death)) { position = death; dead = true; }
                else positions[key] = position!;
            }
            raw.Add(("player:" + key, player.ObserverSlot is >= 0 and <= 9 ? player.ObserverSlot.ToString()! : "?", position!, forward, player.Side, dead, "player"));
        }
        var bombState = game?.Bomb?.State is null or BombStatus.Unknown ? game?.RoundBomb : game.Bomb.State;
        if (Finite(game?.Bomb?.Position) && bombState is BombStatus.Dropped or BombStatus.Planting or BombStatus.Planted or BombStatus.Defusing or BombStatus.Defused)
            raw.Add(("bomb", "B", game!.Bomb!.Position!, null, null, false, bombState == BombStatus.Defused ? "bomb-defused" : "bomb"));
        if (game?.Grenades is { IsDefault: false } grenades)
        {
            var flames = 0;
            foreach (var grenade in grenades.Take(64))
            {
                var owner = context.Players.FirstOrDefault(player => player.SteamId == grenade.OwnerSteamId);
                // Upstream src/gsi.js distinguishes deployed smoke by effecttime, but flash by lifetime.
                var kind = grenade.Type == GrenadeKind.Smoke && grenade.EffectTimeSeconds is > 0 ? "smoke" :
                    grenade.Type == GrenadeKind.Flashbang && grenade.LifetimeSeconds is >= 1.4 ? "flash" : "grenade";
                if (kind == "flash" && Finite(grenade.Position))
                {
                    // Remember the ID for this round after expiry: replayed snapshots cannot restart the pulse.
                    if (flashes.Count < 128) flashes.TryAdd(grenade.Id, new(grenade.Position!, owner?.Side, context.Now));
                }
                else if (Finite(grenade.Position) && grenade.Type != GrenadeKind.Inferno) raw.Add(("grenade:" + grenade.Id, grenade.Type?.ToString() ?? "?", grenade.Position!, null, owner?.Side, false, kind));
                if (grenade.Flames is { IsDefault: false } points)
                    foreach (var point in points.Where(point => Finite(point.Position)).Take(Math.Max(0, 256 - flames)))
                    {
                        raw.Add(($"grenade:{grenade.Id}:flame:{point.Id}", "Flame", point.Position, null, owner?.Side, false, "flame"));
                        flames++;
                    }
            }
        }
        foreach (var (id, flash) in flashes)
            if (context.Now >= flash.StartedAt && context.Now - flash.StartedAt < TimeSpan.FromMilliseconds(2200))
                raw.Add(("flash:" + id, "Flashbang", flash.Position, null, flash.Side, false, "flash"));
        var overview = context.ResolveMedia(context.Text("radar", "overview"));
        var calibrated = overview is not null && context.Text("radar", "mapName") == game?.Map &&
            Read(context, "minX", out var minX) && Read(context, "maxX", out var maxX) &&
            Read(context, "minY", out var minY) && Read(context, "maxY", out var maxY) && maxX > minX && maxY > minY;
        var observedPosition = game?.ObservedPlayer?.Position ?? context.Players.FirstOrDefault(player => player.SteamId is not null && player.SteamId == game?.ObservedPlayer?.SteamId)?.Position;
        double? observedZ = Finite(observedPosition) ? observedPosition!.Z : null;
        if (fresh && game?.ObservedPlayer?.SteamId is { } observedId && !radar!.Players.IsDefault && radar.Players.FirstOrDefault(player => player.SteamId == observedId)?.Position is { } observedHlae && Finite(observedHlae)) observedZ = observedHlae.Z;
        var selected = context.RadarCatalog.Select(game?.Map, observedZ);
        var bolt = calibrated ? null : context.BoltRadarCatalog.Find(game?.Map);
        var boltImage = bolt is not null ? context.ResolveAsset(bolt.Image) : null;
        if (boltImage is null) bolt = null;
        RadarLayer? layer = null;
        if (bolt is not null) overview = boltImage;
        else if (!calibrated && selected is { } automatic && context.ResolveAsset(automatic.Layer.Image) is { } automaticImage)
        { overview = automaticImage; layer = automatic.Layer; }
        double x0 = 0, x1 = 1, y0 = 0, y1 = 1;
        if (calibrated)
        {
            Read(context, "minX", out x0); Read(context, "maxX", out x1); Read(context, "minY", out y0); Read(context, "maxY", out y1);
        }
        else if (layer is not null)
        {
            x0 = layer.PosX; x1 = layer.PosX + layer.Width * layer.Scale;
            y1 = layer.PosY; y0 = layer.PosY - layer.Height * layer.Scale;
        }
        else if (raw.Count > 0)
        {
            x0 = raw.Min(item => item.Position.X) - 256; x1 = raw.Max(item => item.Position.X) + 256;
            y0 = raw.Min(item => item.Position.Y) - 256; y1 = raw.Max(item => item.Position.Y) + 256;
        }
        var markers = raw.Select(item =>
        {
            var point = bolt is not null ? BoltRadarCatalog.Project(bolt, item.Position) : ((item.Position.X - x0) / (x1 - x0) * 100, (y1 - item.Position.Y) / (y1 - y0) * 100);
            var player = item.Kind == "player" ? context.Players.FirstOrDefault(player => "player:" + player.SteamId == item.Key) : null;
            var grenade = item.Kind == "grenade" && game?.Grenades is { IsDefault: false } source ? source.FirstOrDefault(grenade => "grenade:" + grenade.Id == item.Key) : null;
            var projectile = grenade?.Type switch { GrenadeKind.Smoke => "smoke", GrenadeKind.Flashbang => "flashbang", GrenadeKind.HighExplosive => "frag", GrenadeKind.Molotov or GrenadeKind.Incendiary or GrenadeKind.Firebomb => "firebomb", _ => null };
            var image = projectile is not null && item.Side is TeamSide.CounterTerrorists or TeamSide.Terrorists ? context.ResolveAsset($"assets/boltobserv/img/projectile-img-{projectile}-{(item.Side == TeamSide.CounterTerrorists ? "CT" : "T")}.webp") : null;
            var diameter = item.Kind switch { "smoke" => 300, "flame" => 100, "flash" => 290, _ => 0 };
            var worldUnits = bolt?.Metadata?.Resolution * 1024 ?? (layer?.Width * layer?.Scale ?? (x1 - x0));
            return new RadarMarker(item.Key, item.Label, point.Item1, point.Item2,
            Finite(item.Forward) && (item.Forward!.X != 0 || item.Forward.Y != 0) ? Math.Atan2(-item.Forward.Y, item.Forward.X) * 180 / Math.PI : null,
            item.Side, item.Dead, item.Kind, layer?.MinZ is { } minZ && item.Position.Z < minZ ? "lower" : layer?.MaxZ is { } maxZ && item.Position.Z >= maxZ ? "higher" : null)
            {
                MotionKey = $"{resetKey}:{motionGeneration}:{timeline}:{(fresh ? "hlae" : "gsi")}:{overview}:{(bolt is null ? -1 : Array.FindIndex(bolt.Metadata!.Splits, split => item.Position.Z > split.Bounds!.Bottom && item.Position.Z < split.Bounds.Top))}:{item.Dead}:{item.Key}",
                Observed = player?.SteamId is { } id && id == game?.ObservedPlayer?.SteamId,
                BombCarrier = player is not null && EquipmentPresentation.HasBomb(player, game),
                Flashed = player?.Flashed is > 31,
                Image = image,
                Scale = item.Dead || bolt is null ? .7 : BoltRadarCatalog.MarkerScale(bolt, item.Position.Z),
                Diameter = diameter > 0 && double.IsFinite(worldUnits) && worldUnits > 0 ? diameter / worldUnits * 100 : item.Kind == "grenade" ? 4 : 3.92,
                EffectOpacity = item.Kind == "flash" && flashes.TryGetValue(item.Key[6..], out var flash) ? FlashOpacity(context.Now - flash.StartedAt) : 1
            };
        }).Where(item => item.X is >= 0 and <= 100 && item.Y is >= 0 and <= 100).OrderBy(item => item.Kind == "player" ? item.Observed ? 3 : item.Dead ? 1 : 2 : 0).ToArray();
        var label = calibrated ? "Custom overview" : bolt is not null ? $"{bolt.Name} · Boltobserv" : layer is not null && selected is { } chosen ? $"{chosen.Map.Name} · Valve overview" : "Relative positions · map unavailable";
        return new($"{label} · {(fresh ? "HLAE" : "GSI")}", calibrated || layer is not null || bolt is not null ? overview : null, markers, layer is not null && bolt is null ? selected?.Map.Sites : null)
        {
            IsBoltobserv = bolt is not null,
            Buyzones = bolt is not null && (game?.RoundPhase == RoundPhase.FreezeTime || game?.Countdown?.Phase is CountdownPhase.FreezeTime or CountdownPhase.Warmup || game?.Phase == MatchPhase.Warmup) ? context.ResolveAsset(bolt.Buyzones) : null
        };
    }
    private static double FlashOpacity(TimeSpan age) => age.TotalMilliseconds < 100 ? 0 : age.TotalMilliseconds < 1200 ? .8 : Math.Clamp((2200 - age.TotalMilliseconds) / 1000 * .8, 0, .8);
    private static bool Read(BroadcastContext context, string name, out double value) => double.TryParse(context.Text("radar", name), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value) && Math.Abs(value) < 100_000_000;
    private static bool Finite(Vector3State? vector) => vector is not null && double.IsFinite(vector.X) && double.IsFinite(vector.Y) && double.IsFinite(vector.Z) && Math.Abs(vector.X) < 100_000_000 && Math.Abs(vector.Y) < 100_000_000 && Math.Abs(vector.Z) < 100_000_000;
}
