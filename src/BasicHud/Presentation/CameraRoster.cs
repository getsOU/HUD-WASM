using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public static class CameraRoster
{
    public static ManagedTeam? Team(BroadcastContext context, bool right)
    {
        if (context.Match is not { } match) return null;
        if (!context.Metadata.Matches(match)) return right ? match.TeamB : match.TeamA;
        var id = right ? context.Metadata.RightTeamId : context.Metadata.LeftTeamId;
        return id == match.TeamA.Id ? match.TeamA : id == match.TeamB.Id ? match.TeamB : null;
    }
    public static int UpperCount(int count) => (count + 1) / 2;
    public static IReadOnlyList<PlayerState> Players(BroadcastContext context, bool right)
    {
        if (Team(context, right) is not { } team) return [];
        // An explicitly empty verified roster is authoritative, not a side fallback.
        if (context.Metadata.Matches(context.Match) && context.Metadata.Rosters.TryGetValue(team.Id, out var ids) && ids.Count == 0) return [];
        return PlayerPresentation.Ordered(context.TeamPlayers(team));
    }
    public static bool IsScene(string scene) => scene is "roster-left" or "roster-right";
    public static IReadOnlySet<string> VisibleCameras(BroadcastContext context, string scene)
    {
        var options = context.Panel is null ? DisplayOptions.Default : DisplayOptions.FromPanel(context.Panel);
        if (scene == "live") return options.ShowObservedPlayer && context.Game?.ObservedPlayer?.SteamId is { } id ? new HashSet<string>([id], StringComparer.Ordinal) : new HashSet<string>();
        return IsScene(scene) && options.ShowRosters
            ? Players(context, scene == "roster-right").Select(player => player.SteamId).OfType<string>().ToHashSet(StringComparer.Ordinal) : new HashSet<string>();
    }
    public static bool PoolEnabled(BroadcastContext context, string scene)
    {
        var options = context.Panel is null ? DisplayOptions.Default : DisplayOptions.FromPanel(context.Panel);
        return scene == "live" ? options.ShowObservedPlayer : IsScene(scene) && options.ShowRosters;
    }
}
