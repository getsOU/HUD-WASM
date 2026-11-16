using System.Globalization;
using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public sealed record RecordingLayout(int Columns, int Rows, double Scale)
{
    public string Style => $"--list-columns:{Columns};--list-rows:{Rows};--list-scale:{Scale.ToString("0.###", CultureInfo.InvariantCulture)}";
    public static RecordingLayout Cards(int count, int preferredRows = 5, int maxColumns = 4, double baseScale = 1)
    {
        var columns = Math.Clamp((count + preferredRows - 1) / preferredRows, 1, maxColumns);
        var rows = Math.Max(1, (count + columns - 1) / columns);
        return new(columns, rows, baseScale * Math.Min(1, 8.0 / rows) * Math.Min(1, 3.0 / columns));
    }
}

public sealed record RecordedMultikills(string SteamId, string Name, int Rounds, int KillRounds,
    long? DoubleKills, long? TripleKills, long? QuadKills, long? FivePlusKills)
{
    // Public side samples partition observed rounds. Combine those samples once,
    // including historically unassigned rounds, without constructing a current side.
    public static IReadOnlyList<RecordedMultikills> From(IEnumerable<HudPlayerSideStatistics> samples) => samples
        .GroupBy(p => p.SteamId, StringComparer.Ordinal).Select(group =>
        {
            var covered = group.Sum(p => p.KillRounds);
            long? Sum(Func<HudPlayerSideStatistics, int> pick) => covered > 0 ? group.Sum(p => (long)pick(p)) : null;
            return new RecordedMultikills(group.Key, group.First().Name ?? "Unknown", group.Sum(p => p.Rounds), covered,
                Sum(p => p.DoubleKills), Sum(p => p.TripleKills), Sum(p => p.QuadKills), Sum(p => p.FivePlusKills));
        }).ToArray();
}
