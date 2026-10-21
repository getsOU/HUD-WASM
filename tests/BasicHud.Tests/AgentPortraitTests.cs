using System.Security.Cryptography;
using System.Text.Json;
using BasicHud.Core;
using SOU.Live.Contracts;

static class AgentPortraitTests
{
    public static void Run(Action<bool, string> check)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var webroot = Path.Combine(root, "src/BasicHud/wwwroot");
        var manifest = File.ReadAllText(Path.Combine(root, "LICENSES/agent-portraits.json"));
        check(manifest == File.ReadAllText(Path.Combine(webroot, "assets/agents/catalog.json")), "agent source inventory is included with the package");
        using var document = JsonDocument.Parse(manifest);
        var assets = document.RootElement.GetProperty("files").EnumerateArray().ToArray();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var side in new[] { TeamSide.CounterTerrorists, TeamSide.Terrorists })
        {
            var prefix = side == TeamSide.CounterTerrorists ? "ct" : "t";
            var portraits = Enumerable.Range(0, 10).Select(slot => AgentPortraits.For(new("player", "Player", side, null, null) { ObserverSlot = slot })).ToArray();
            check(portraits.Distinct().Count() == 10 && portraits.All(path => path?.StartsWith($"assets/agents/{prefix}-", StringComparison.Ordinal) == true), $"{prefix} observer digits 0 through 9 each have a distinct side-correct agent");
            foreach (var portrait in portraits) paths.Add(portrait!);
        }
        check(assets.Length == 20 && assets.Select(asset => asset.GetProperty("path").GetString()!).ToHashSet(StringComparer.Ordinal).SetEquals(paths) && assets.Select(asset => asset.GetProperty("sha256").GetString()).Distinct().Count() == 20, "all twenty slot placeholders have distinct artwork and exact source records");
        check(assets.All(asset =>
        {
            var bytes = File.ReadAllBytes(Path.Combine(webroot, asset.GetProperty("path").GetString()!));
            return bytes.Length == asset.GetProperty("bytes").GetInt32() && Convert.ToHexStringLower(SHA256.HashData(bytes)) == asset.GetProperty("sha256").GetString();
        }), "all bundled agent PNGs match their source hashes");
        var player = new PlayerState("player", "Player", TeamSide.CounterTerrorists, null, null) { ObserverSlot = 1 };
        check(AgentPortraits.For(player with { Side = null }) is null && AgentPortraits.For(player with { Side = TeamSide.Unknown }) is null, "unknown sides never receive an assumed CT or T agent");
        check(new int?[] { null, -1, 10, int.MaxValue }.All(slot => AgentPortraits.For(player with { ObserverSlot = slot }) is null), "missing or invalid observer slots never impersonate another numbered player");
    }
}
