using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public static class MatchPresentation
{
    public static (string Label, string Description) MapPick(ManagedMatch match, ManagedMatchMap map)
    {
        var decisions = match.Vetos.Where(veto => !string.IsNullOrEmpty(map.Id) && veto.MapId == map.Id).ToArray();
        if (decisions.Length != 1) return ("Pick unknown", "No unambiguous pick decision supplied");
        var decision = decisions[0];
        if (decision.PlayNumber is { } number && number != map.Number ||
            !string.IsNullOrEmpty(decision.GameName) && !string.IsNullOrEmpty(map.GameName) && MapKey(decision.GameName) != MapKey(map.GameName))
            return ("Pick unknown", "Conflicting map decision");
        if (decision.Type == MapDecisionKind.Decider) return ("DECIDER", "Decider map");
        if (decision.Type != MapDecisionKind.Pick) return ("Pick unknown", "No pick decision supplied");
        var team = decision.TeamId == match.TeamA.Id ? match.TeamA : decision.TeamId == match.TeamB.Id ? match.TeamB : null;
        if (team is null || match.TeamA.Id == match.TeamB.Id) return ("Pick unassigned", "Pick team not assigned");
        return ($"PICK · {(string.IsNullOrWhiteSpace(team.ShortName) ? team.Name : team.ShortName)}", $"Picked by {team.Name}");
    }

    public static bool ShowHistory(GameState? game)
    {
        if (game is null || game.RoundPhase == RoundPhase.Over ||
            game.Countdown?.Phase is CountdownPhase.Live or CountdownPhase.Bomb or CountdownPhase.Defuse or CountdownPhase.Over ||
            game.Bomb?.State is BombStatus.Planting or BombStatus.Planted or BombStatus.Defusing ||
            game.RoundBomb is BombStatus.Planting or BombStatus.Planted or BombStatus.Defusing) return false;
        return game.RoundPhase == RoundPhase.FreezeTime || game.Phase == MatchPhase.Intermission ||
            game.Countdown?.Phase is CountdownPhase.FreezeTime or CountdownPhase.Paused or CountdownPhase.TimeoutCounterTerrorists or CountdownPhase.TimeoutTerrorists;
    }

    public static string Caption(BroadcastContext context, string? caption) => !string.IsNullOrWhiteSpace(caption)
        ? caption.Trim() : context.Match?.Competition?.Name?.Trim() ?? "";

    public static string RoundLabel(GameState? game, TimerView timer)
    {
        if (timer.IsBomb || game?.Countdown?.Phase is CountdownPhase.Warmup or CountdownPhase.Paused or CountdownPhase.TimeoutCounterTerrorists or CountdownPhase.TimeoutTerrorists or CountdownPhase.GameOver) return timer.Label;
        if (game?.Phase == MatchPhase.Warmup) return "WARMUP";
        if (game?.Phase == MatchPhase.GameOver) return "FINAL";
        return game?.RoundNumber is > 0 ? $"ROUND {game.RoundNumber}" : "";
    }

    public static int? SeriesTarget(ManagedMatch? match) => match?.BestOf is > 1 and <= 9 && match.BestOf % 2 == 1 ? match.BestOf / 2 + 1 : null;

    public static bool IsCurrentMap(GameState? game, ManagedMatchMap map)
    {
        if (string.IsNullOrWhiteSpace(game?.Map) || string.IsNullOrWhiteSpace(map.GameName)) return false;
        var source = MapKey(game.Map);
        return string.Equals(source, map.GameName, StringComparison.Ordinal);
    }

    public static string? MapKey(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;
        if (!source.Contains('/')) return source;
        var parts = source.Split('/');
        return parts is ["workshop", var id, var name] && id.Length > 0 && id.All(char.IsAsciiDigit) && name.Length > 0 ? name : null;
    }

    public static IReadOnlyList<RoundResult> History(GameState? game, bool complete = false) => game?.RoundWins is { IsDefault: false } rounds
        ? rounds.Where(result => result.RoundNumber > 0 && (game.RoundNumber is null || result.RoundNumber <= game.RoundNumber))
            .GroupBy(result => result.RoundNumber).Select(group => group.Last()).OrderBy(result => result.RoundNumber).TakeLast(complete ? int.MaxValue : 12).ToArray() : [];

    public static string ReasonIcon(RoundWinReason reason) => reason switch
    {
        RoundWinReason.Elimination => "dead",
        RoundWinReason.BombExploded => "bomb",
        RoundWinReason.BombDefused => "kit",
        RoundWinReason.TimeExpired => "time",
        RoundWinReason.HostageRescued => "hostage",
        _ => "unknown"
    };

    public static string Reason(RoundWinReason reason) => reason switch
    {
        RoundWinReason.Elimination => "Elimination",
        RoundWinReason.BombExploded => "Bomb exploded",
        RoundWinReason.BombDefused => "Bomb defused",
        RoundWinReason.TimeExpired => "Time expired",
        RoundWinReason.HostageRescued => "Hostage rescued",
        _ => "Result supplied without a reason"
    };
}
