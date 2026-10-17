using SOU.Live.Contracts;

namespace BasicHud.Core;

public sealed record EconomyView(string Alive, string Money, string Equipment, string Kits, string Utility);
public static class LiveProjection
{
    public static EconomyView ForSide(GameState? game, TeamSide side)
    {
        if (game is null || game.Players.IsDefault || game.AvailableSections.IsDefault || !game.AvailableSections.Contains(GsiSection.AllPlayers))
            return new("— (roster unknown)", "—", "—", "—", "—");
        var players = game.Players.Where(player => player.Side == side).ToArray();
        var result = Economy(players);
        return game.Players.Any(player => player.Side is null or TeamSide.Unknown)
            ? result with { Alive = $"{players.Count(player => player.Health is > 0)} known · side unknown", Money = "—", Equipment = "—", Kits = "—", Utility = "—" }
            : result;
    }
    public static EconomyView Economy(IEnumerable<PlayerState> source)
    {
        var players = source.ToArray();
        string Sum(Func<PlayerState, int?> pick, string prefix = "") => players.Length == 0 || players.Any(player => pick(player) is null or < 0) ? "—" : prefix + players.Sum(player => (long)pick(player)!.Value).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        var knownHp = players.Count(player => player.Health is not null);
        var alive = players.Count(player => player.Health is > 0);
        return new(players.Length == 0 ? "—" : knownHp == players.Length ? $"{alive}/{players.Length}" : $"{alive} known · {players.Length - knownHp} unknown",
            Sum(player => player.Money, "$"), Sum(player => player.EquipmentValue, "$"),
            players.Length == 0 || players.Any(player => player.DefuseKit is null) ? "—" : players.Count(player => player.DefuseKit == true).ToString(),
            players.Length == 0 || players.Any(player => player.Weapons is null || player.Weapons.Value.IsDefault) ? "—" : players.Sum(player => player.Weapons!.Value.Count(weapon => weapon.Type == WeaponCategory.Grenade)).ToString());
    }
    public static string RoundMessage(GameState? game) => game?.RoundPhase == RoundPhase.Over && game.RoundWinner is TeamSide.CounterTerrorists or TeamSide.Terrorists
        ? $"{(game.RoundWinner == TeamSide.CounterTerrorists ? "CT" : "T")} WIN · {game.RoundWinReason?.ToString() ?? "—"}" : "";
}

public sealed record KillView(string Key, KillFeedEntry Event);
public sealed class KillFeedProjection
{
    private string resetKey = "";
    private readonly Dictionary<string, DateTimeOffset> seen = [];
    public IReadOnlyList<KillView> Project(LiveSnapshot? snapshot, DateTimeOffset now)
    {
        var source = snapshot?.Game is { KillFeed.IsDefault: false } game ? game.KillFeed : [];
        var latest = source.LastOrDefault();
        var nextKey = $"{snapshot?.SessionId}:{snapshot?.Game?.Map}:{latest?.ConnectionId}:{latest?.Epoch}";
        if (nextKey != resetKey) { seen.Clear(); resetKey = nextKey; }
        var entries = source.Where(entry => entry.ConnectionId == latest?.ConnectionId && entry.Epoch == latest?.Epoch)
            .GroupBy(entry => (entry.ConnectionId, entry.Epoch, entry.Id)).Select(group => group.Last()).OrderBy(entry => entry.Id).TakeLast(64).ToArray();
        var keys = entries.Select(entry => $"{entry.ConnectionId:N}:{entry.Epoch}:{entry.Id}").ToHashSet(StringComparer.Ordinal);
        foreach (var key in seen.Keys.Where(key => !keys.Contains(key)).ToArray()) seen.Remove(key);
        foreach (var key in keys) seen.TryAdd(key, now);
        return entries.Where(entry => now - seen[$"{entry.ConnectionId:N}:{entry.Epoch}:{entry.Id}"] <= TimeSpan.FromSeconds(8))
            .TakeLast(6).Select(entry => new KillView($"{entry.ConnectionId:N}:{entry.Epoch}:{entry.Id}", entry)).ToArray();
    }
}
