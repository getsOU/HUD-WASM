namespace BasicHud.Core;

public sealed class RadarCatalogDocument
{
    public int SchemaVersion { get; set; }
    public RadarMap[] Maps { get; set; } = [];
}
public sealed class RadarMap
{
    public string Map { get; set; } = "";
    public string Name { get; set; } = "";
    public RadarLayer[] Layers { get; set; } = [];
    public RadarSite[] Sites { get; set; } = [];
}
public sealed record RadarSite(string Label, double X, double Y);
public sealed class RadarLayer
{
    public string Id { get; set; } = "";
    public string Image { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public double PosX { get; set; }
    public double PosY { get; set; }
    public double Scale { get; set; }
    public double? MinZ { get; set; }
    public double? MaxZ { get; set; }
    public bool Contains(double z) => (MinZ is null || z >= MinZ) && (MaxZ is null || z < MaxZ);
}
public sealed class RadarCatalog
{
    public static RadarCatalog Empty { get; } = new(new Dictionary<string, RadarMap>(StringComparer.Ordinal));
    private readonly IReadOnlyDictionary<string, RadarMap> maps;
    private RadarCatalog(IReadOnlyDictionary<string, RadarMap> maps) => this.maps = maps;
    public static RadarCatalog Bind(RadarCatalogDocument? document)
    {
        if (document?.SchemaVersion != 1 || document.Maps is null || document.Maps.Length > 64) return Empty;
        var maps = new Dictionary<string, RadarMap>(StringComparer.Ordinal);
        foreach (var map in document.Maps)
        {
            if (map is null || string.IsNullOrWhiteSpace(map.Map) || map.Layers is null || map.Layers.Length is 0 or > 8 || maps.ContainsKey(map.Map)) return Empty;
            if (map.Layers.Any(layer => !Valid(layer)) || map.Sites is null || map.Sites.Length > 8 || map.Sites.Any(site => site is null || site.Label is not ("A" or "B") || !double.IsFinite(site.X) || !double.IsFinite(site.Y) || site.X is < 0 or > 1 || site.Y is < 0 or > 1)) return Empty;
            maps.Add(map.Map, map);
        }
        return new(maps);
    }
    public (RadarMap Map, RadarLayer Layer, bool FloorKnown)? Select(string? map, double? z)
    {
        if (MatchPresentation.MapKey(map) is not { } key || !maps.TryGetValue(key, out var found)) return null;
        var known = z is { } height && double.IsFinite(height);
        var layer = known ? found.Layers.FirstOrDefault(layer => layer.Contains(z!.Value)) : found.Layers.FirstOrDefault(layer => layer.Id == "main") ?? found.Layers[0];
        return layer is null ? null : (found, layer, known || found.Layers.All(layer => layer.MinZ is null && layer.MaxZ is null));
    }
    private static bool Valid(RadarLayer? layer) => layer is not null && !string.IsNullOrWhiteSpace(layer.Id) && !string.IsNullOrWhiteSpace(layer.Image)
        && layer.Image.StartsWith("assets/radar/", StringComparison.Ordinal) && layer.Image.EndsWith(".png", StringComparison.Ordinal)
        && layer.Image.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-' or '/' or '.')
        && !layer.Image.Contains("..", StringComparison.Ordinal) && !layer.Image.Contains('\\') && !layer.Image.Contains(':') && !layer.Image.Contains('?') && !layer.Image.Contains('#')
        && layer.Width is > 0 and <= 8192 && layer.Height is > 0 and <= 8192 && double.IsFinite(layer.PosX) && double.IsFinite(layer.PosY)
        && double.IsFinite(layer.Scale) && layer.Scale is > 0 and < 100 && Math.Abs(layer.PosX) < 100_000_000 && Math.Abs(layer.PosY) < 100_000_000
        && (layer.MinZ is null || double.IsFinite(layer.MinZ.Value)) && (layer.MaxZ is null || double.IsFinite(layer.MaxZ.Value))
        && (layer.MinZ is null || layer.MaxZ is null || layer.MaxZ > layer.MinZ);
}
