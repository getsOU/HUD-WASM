using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public sealed class BoltRadarDocument
{
    public int SchemaVersion { get; set; }
    public BoltRadarMap[] Maps { get; set; } = [];
}
public sealed class BoltRadarMap
{
    public string Map { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string Buyzones { get; set; } = "";
    public string Logos { get; set; } = "";
    public BoltMapMetadata? Metadata { get; set; }
}
public sealed class BoltMapMetadata
{
    public double Resolution { get; set; }
    public BoltOffset? Offset { get; set; }
    public BoltSplit[] Splits { get; set; } = [];
    public BoltVersion? Version { get; set; }
    public BoltZRange? ZRange { get; set; }
}
public sealed class BoltVersion { public int Format { get; set; } }
public sealed class BoltOffset { public double X { get; set; } public double Y { get; set; } }
public sealed class BoltBounds { public double Top { get; set; } public double Bottom { get; set; } }
public sealed class BoltSplit { public BoltBounds? Bounds { get; set; } public BoltOffset? Offset { get; set; } public BoltZRange? ZRange { get; set; } }
public sealed class BoltZRange { public double Min { get; set; } public double Max { get; set; } }

/// <summary>Original Boltobserv format-4 calibration and object-specific split projection.</summary>
public sealed class BoltRadarCatalog
{
    public static BoltRadarCatalog Empty { get; } = new(new Dictionary<string, BoltRadarMap>(StringComparer.Ordinal));
    private readonly IReadOnlyDictionary<string, BoltRadarMap> maps;
    private BoltRadarCatalog(IReadOnlyDictionary<string, BoltRadarMap> maps) => this.maps = maps;
    public BoltRadarMap? Find(string? name) => MatchPresentation.MapKey(name) is { } key && maps.TryGetValue(key, out var map) ? map : null;
    public static BoltRadarCatalog Bind(BoltRadarDocument? document)
    {
        if (document?.SchemaVersion != 1 || document.Maps is null || document.Maps.Length > 64) return Empty;
        var maps = new Dictionary<string, BoltRadarMap>(StringComparer.Ordinal);
        foreach (var map in document.Maps)
        {
            if (map is null || string.IsNullOrWhiteSpace(map.Map) || !Path(map.Image) || !Path(map.Buyzones) || !Path(map.Logos) ||
                map.Metadata is not { Resolution: > 0 and < 100, Version.Format: 4 } metadata || !double.IsFinite(metadata.Resolution) || !Offset(metadata.Offset) ||
                metadata.Splits is null || metadata.Splits.Length > 8 || metadata.Splits.Any(split => split is null || !Offset(split.Offset) || split.Bounds is not { } bounds ||
                    !double.IsFinite(bounds.Top) || !double.IsFinite(bounds.Bottom) || bounds.Top <= bounds.Bottom) || !maps.TryAdd(map.Map, map)) return Empty;
        }
        return new(maps);
    }
    // The original metadata's split ranges are strict at BOTH ends. They are not floor-image selectors.
    public static (double X, double Y) Project(BoltRadarMap map, Vector3State position)
    {
        var metadata = map.Metadata!;
        var split = metadata.Splits.FirstOrDefault(split => position.Z > split.Bounds!.Bottom && position.Z < split.Bounds.Top);
        return ((position.X + metadata.Offset!.X) / metadata.Resolution / 1024 * 100 + (split?.Offset?.X ?? 0),
            100 - ((position.Y + metadata.Offset.Y) / metadata.Resolution / 1024 * 100 + (split?.Offset?.Y ?? 0)));
    }
    private static bool Offset(BoltOffset? offset) => offset is not null && double.IsFinite(offset.X) && double.IsFinite(offset.Y) && Math.Abs(offset.X) < 100_000_000 && Math.Abs(offset.Y) < 100_000_000;
    // Boltobserv loopFast.js: scale both the dot and its separate upright label.
    public static double MarkerScale(BoltRadarMap map, double z)
    {
        var metadata = map.Metadata!;
        var range = metadata.Splits.FirstOrDefault(split => z > split.Bounds!.Bottom && z < split.Bounds.Top)?.ZRange ?? metadata.ZRange;
        if (!double.IsFinite(z) || range is null || !double.IsFinite(range.Min) || !double.IsFinite(range.Max) || range.Max <= range.Min) return .7;
        var height = Math.Clamp((z - range.Min) / (range.Max - range.Min), 0, 1);
        return .7 * ((height - .5) / 2 + 1);
    }
    private static bool Path(string? path) => path is not null && path.StartsWith("assets/boltobserv/maps/", StringComparison.Ordinal) && path.EndsWith(".png", StringComparison.Ordinal) &&
        !path.Contains("..", StringComparison.Ordinal) && path.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-' or '/' or '.');
}
