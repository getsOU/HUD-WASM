using System.Globalization;
using SOU.Live.Contracts;

namespace BasicHud.Core;

public sealed record RadarMarker(string Key, string Label, double X, double Y, double? Angle, TeamSide? Side, bool Dead, string Kind, string? Elevation = null);
public sealed record RadarView(string Label, string? Overview, IReadOnlyList<RadarMarker> Markers, IReadOnlyList<RadarSite>? Sites = null);
public sealed class RadarProjection
{
    private string resetKey = "";
    private string timeline = "";
    private double lastGameTime = double.MinValue;
    private long lastSequence = long.MinValue;
    private RoundPhase? previousPhase;
    private string lastFrame = "", blockedFrame = "";
    private readonly Dictionary<string, Vector3State> positions = [];
    private readonly Dictionary<string, Vector3State> deadPositions = [];

    public RadarView Project(BroadcastContext context)
    {
        var game = context.Game;
        var nextReset = $"{context.Snapshot?.SessionId}:{game?.Map}:{game?.RoundNumber}";
        if (resetKey != nextReset || game?.RoundPhase == RoundPhase.FreezeTime && previousPhase != RoundPhase.FreezeTime)
        { positions.Clear(); deadPositions.Clear(); timeline = ""; lastGameTime = double.MinValue; lastSequence = long.MinValue; resetKey = nextReset; blockedFrame = lastFrame; }
        previousPhase = game?.RoundPhase;
        var radar = game?.Radar;
        var frame = $"{radar?.ConnectionId}:{radar?.Epoch}:{radar?.Sequence}";
        var fresh = radar is not null && radar.ConnectionId != Guid.Empty && radar.Epoch >= 0 && radar.Sequence >= 0 && double.IsFinite(radar.GameTimeSeconds)
            && frame != blockedFrame && context.Now >= radar.ReceivedAt && context.Now - radar.ReceivedAt <= TimeSpan.FromMilliseconds(750);
        if (fresh)
        {
            var nextTimeline = $"{radar!.ConnectionId}:{radar.Epoch}";
            if (nextTimeline != timeline) { positions.Clear(); deadPositions.Clear(); timeline = nextTimeline; lastGameTime = double.MinValue; lastSequence = long.MinValue; }
            if (radar.GameTimeSeconds < lastGameTime || radar.Sequence < lastSequence) fresh = false;
            else { lastGameTime = radar.GameTimeSeconds; lastSequence = radar.Sequence; lastFrame = frame; }
        }
        var raw = new List<(string Key, string Label, Vector3State Position, Vector3State? Forward, TeamSide? Side, bool Dead, string Kind)>();
        var activeIds = context.Players.Take(64).Where(player => player.SteamId is not null).Select(player => player.SteamId!).ToHashSet(StringComparer.Ordinal);
        foreach (var old in positions.Keys.Where(id => !activeIds.Contains(id)).ToArray()) positions.Remove(old);
        foreach (var old in deadPositions.Keys.Where(id => !activeIds.Contains(id)).ToArray()) deadPositions.Remove(old);
        foreach (var player in context.Players.Take(64))
        {
            var hlae = fresh && player.SteamId is { } id && !radar!.Players.IsDefault ? radar.Players.FirstOrDefault(item => item.SteamId == id) : null;
            var position = hlae?.Position ?? player.Position;
            var forward = hlae?.Forward ?? player.Forward;
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
        if (Finite(game?.Bomb?.Position)) raw.Add(("bomb", "B", game!.Bomb!.Position!, null, null, false, "bomb"));
        if (game?.Grenades is { IsDefault: false } grenades)
        {
            var flames = 0;
            foreach (var grenade in grenades.Take(64))
            {
                if (Finite(grenade.Position)) raw.Add(("grenade:" + grenade.Id, grenade.Type?.ToString() ?? "?", grenade.Position!, null, null, false, "grenade"));
                if (grenade.Flames is { IsDefault: false } points)
                    foreach (var point in points.Where(point => Finite(point.Position)).Take(Math.Max(0, 256 - flames)))
                    {
                        raw.Add(($"grenade:{grenade.Id}:flame:{point.Id}", "Flame", point.Position, null, null, false, "flame"));
                        flames++;
                    }
            }
        }
        var overview = context.ResolveMedia(context.Text("radar", "overview"));
        var calibrated = overview is not null && context.Text("radar", "mapName") == game?.Map &&
            Read(context, "minX", out var minX) && Read(context, "maxX", out var maxX) &&
            Read(context, "minY", out var minY) && Read(context, "maxY", out var maxY) && maxX > minX && maxY > minY;
        var observedPosition = game?.ObservedPlayer?.Position ?? context.Players.FirstOrDefault(player => player.SteamId is not null && player.SteamId == game?.ObservedPlayer?.SteamId)?.Position;
        double? observedZ = Finite(observedPosition) ? observedPosition!.Z : null;
        if (fresh && game?.ObservedPlayer?.SteamId is { } observedId && !radar!.Players.IsDefault && radar.Players.FirstOrDefault(player => player.SteamId == observedId)?.Position is { } observedHlae && Finite(observedHlae)) observedZ = observedHlae.Z;
        var selected = context.RadarCatalog.Select(game?.Map, observedZ);
        RadarLayer? layer = null;
        if (!calibrated && selected is { } automatic && context.ResolveAsset(automatic.Layer.Image) is { } automaticImage)
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
        var markers = raw.Select(item => new RadarMarker(item.Key, item.Label, (item.Position.X - x0) / (x1 - x0) * 100,
            (y1 - item.Position.Y) / (y1 - y0) * 100,
            Finite(item.Forward) && (item.Forward!.X != 0 || item.Forward.Y != 0) ? Math.Atan2(-item.Forward.Y, item.Forward.X) * 180 / Math.PI : null,
            item.Side, item.Dead, item.Kind, layer?.MinZ is { } minZ && item.Position.Z < minZ ? "lower" : layer?.MaxZ is { } maxZ && item.Position.Z >= maxZ ? "higher" : null)).Where(item => item.X is >= 0 and <= 100 && item.Y is >= 0 and <= 100).ToArray();
        var label = calibrated ? "Custom overview" : layer is not null && selected is { } chosen ? $"{chosen.Map.Name} · {layer.Id}{(chosen.FloorKnown ? "" : " · floor unknown")}" : "Relative positions · map unavailable";
        return new($"{label} · {(fresh ? "HLAE" : "GSI")}", calibrated || layer is not null ? overview : null, markers, layer is not null ? selected?.Map.Sites : null);
    }
    private static bool Read(BroadcastContext context, string name, out double value) => double.TryParse(context.Text("radar", name), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value) && Math.Abs(value) < 100_000_000;
    private static bool Finite(Vector3State? vector) => vector is not null && double.IsFinite(vector.X) && double.IsFinite(vector.Y) && double.IsFinite(vector.Z) && Math.Abs(vector.X) < 100_000_000 && Math.Abs(vector.Y) < 100_000_000 && Math.Abs(vector.Z) < 100_000_000;
}
