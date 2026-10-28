using System.Globalization;
using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public sealed record SideView(TeamSide Side, string Name, string Score, IReadOnlyList<PlayerView> Players);
public sealed record PlayerView(string Key, string Name, TeamSide? Side, string Slot, string Health,
    double? HealthPercent, string Armor, string Money, string Kda, string Weapon, string Ammo, bool IsDead);
public sealed record TimerView(string Label, string Value, bool IsBomb);
public sealed record HudView(SideView Ct, SideView T, string Map, string Round, string Phase,
    TimerView Timer, PlayerView? Observed, DataFreshness Freshness, bool HasGame)
{
    public static HudView Empty { get; } = From(null);

    public static HudView From(LiveSnapshot? snapshot)
    {
        var game = snapshot?.Game;
        var match = snapshot?.ManagedMatch ?? game?.ManagedMatch;
        var players = game is not null && !game.Players.IsDefault ? game.Players : [];
        SideView Side(TeamSide side, SideState? source, TeamSideResolution? resolution) => new(side,
            ResolveTeamName(match, resolution, side) ?? SideName(source?.Name, side),
            Number(source?.Score),
            players.Where(player => player.Side == side)
                .OrderBy(player => ObserverOrder(player.ObserverSlot))
                .ThenBy(player => player.SteamId, StringComparer.Ordinal)
                .Select(Player).ToArray());

        return new(Side(TeamSide.CounterTerrorists, game?.Ct, match?.Ct),
            Side(TeamSide.Terrorists, game?.T, match?.T), Text(game?.Map),
            game?.RoundNumber is > 0 ? $"ROUND {game.RoundNumber}" : "ROUND —",
            FormatPhase(game), CreateTimer(game), game?.ObservedPlayer is { } observed ? Player(observed) : null,
            snapshot?.Freshness ?? DataFreshness.Waiting, game is not null);
    }

    private static string? ResolveTeamName(ManagedMatch? match, TeamSideResolution? resolution, TeamSide expected)
    {
        if (match is null || resolution is null || resolution.Side != expected ||
            resolution.Status is not (TeamRecognitionStatus.Recognized or TeamRecognitionStatus.Inferred or TeamRecognitionStatus.Manual)) return null;
        // IDs are opaque and case-sensitive. Side mapping is supplied by sOU, never inferred here.
        var team = resolution.TeamId == match.TeamA.Id ? match.TeamA : resolution.TeamId == match.TeamB.Id ? match.TeamB : null;
        return string.IsNullOrWhiteSpace(team?.Name) ? null : team.Name;
    }

    private static int ObserverOrder(int? slot) => slot is >= 1 and <= 9 ? slot.Value : slot == 0 ? 10 : int.MaxValue;
    private static string SideName(string? name, TeamSide side) => SourceText.Name(name, side == TeamSide.CounterTerrorists ? "CT" : "T") switch
    { "COUNTER-TERRORISTS" => "CT", "TERRORISTS" => "T", var value => value };

    private static PlayerView Player(PlayerState source)
    {
        var active = EquipmentPresentation.Active(source);
        var name = active?.Name;
        if (name?.StartsWith("weapon_", StringComparison.Ordinal) == true) name = name[7..];
        return new(source.SteamId ?? string.Empty, SourceText.Name(source.Name), source.Side,
            source.ObserverSlot is >= 0 and <= 9 ? Number(source.ObserverSlot) : "—",
            Number(source.Health), source.Health is { } health ? Math.Clamp((double)health, 0, 100) : null,
            Number(source.Armor), source.Money is { } money ? "$" + money.ToString("N0", CultureInfo.InvariantCulture) : "—",
            $"{Number(source.Stats?.Kills)} / {Number(source.Stats?.Deaths)} / {Number(source.Stats?.Assists)}",
            active is null ? "" : Text(name, "Unknown weapon"), active is null ? "" : $"{Number(active.AmmoClip)} / {Number(active.AmmoReserve)}", source.Health is <= 0);
    }

    private static string FormatPhase(GameState? game) => game?.RoundPhase switch
    {
        RoundPhase.FreezeTime => "FREEZE TIME",
        RoundPhase.Live => "LIVE",
        RoundPhase.Over => "ROUND OVER",
        _ => game?.Phase switch
        {
            MatchPhase.Warmup => "WARMUP",
            MatchPhase.Live => "LIVE",
            MatchPhase.Intermission => "INTERMISSION",
            MatchPhase.GameOver => "GAME OVER",
            _ => "—"
        }
    };

    private static TimerView CreateTimer(GameState? game)
    {
        var bombState = game?.Bomb?.State is null or BombStatus.Unknown ? game?.RoundBomb : game.Bomb.State;
        if (bombState is BombStatus.Defused or BombStatus.Exploded)
            return new(bombState == BombStatus.Defused ? "BOMB DEFUSED" : "BOMB EXPLODED", Time(game?.Bomb?.SecondsRemaining), true);
        if (bombState is BombStatus.Planted or BombStatus.Planting or BombStatus.Defusing)
        {
            var seconds = game?.Bomb?.SecondsRemaining;
            if (seconds is null && (bombState == BombStatus.Planted && game?.Countdown?.Phase == CountdownPhase.Bomb ||
                bombState == BombStatus.Defusing && game?.Countdown?.Phase == CountdownPhase.Defuse)) seconds = game.Countdown.SecondsRemaining;
            return new(bombState switch { BombStatus.Defusing => "DEFUSING", BombStatus.Planting => "PLANTING", _ => "BOMB" }, Time(seconds), true);
        }
        return new(game?.Countdown?.Phase switch
        {
            CountdownPhase.Warmup => "WARMUP",
            CountdownPhase.FreezeTime => "FREEZE",
            CountdownPhase.Live => "ROUND TIME",
            CountdownPhase.Over => "ROUND OVER",
            CountdownPhase.Bomb => "BOMB",
            CountdownPhase.Defuse => "DEFUSE",
            CountdownPhase.TimeoutCounterTerrorists => "CT TIMEOUT",
            CountdownPhase.TimeoutTerrorists => "T TIMEOUT",
            CountdownPhase.Paused => "PAUSED",
            CountdownPhase.GameOver => "GAME OVER",
            _ => "TIME"
        }, Time(game?.Countdown?.SecondsRemaining), game?.Countdown?.Phase is CountdownPhase.Bomb or CountdownPhase.Defuse);
    }

    public static string Time(double? seconds)
    {
        if (seconds is not { } value || !double.IsFinite(value) || value < 0 || value > int.MaxValue) return "—";
        var total = (long)Math.Ceiling(value);
        return $"{total / 60}:{total % 60:00}";
    }
    public static string Text(string? value, string fallback = "—") => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "—";
}
