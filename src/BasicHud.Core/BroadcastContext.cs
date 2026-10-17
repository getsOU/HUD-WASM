using System.Globalization;
using System.Text.Json;
using SOU.Live.Contracts;

namespace BasicHud.Core;

public sealed record BroadcastContext(LiveSnapshot? Snapshot, HudPanelState? Panel, MetadataCatalog Metadata,
    DateTimeOffset Now, Func<string?, string?> ResolveMedia)
{
    public static BroadcastContext Empty { get; } = new(null, null, MetadataCatalog.Empty, DateTimeOffset.UtcNow, _ => null);
    public GameState? Game => Snapshot?.Game;
    public ManagedMatch? Match => Snapshot?.ManagedMatch ?? Game?.ManagedMatch;
    public IReadOnlyList<PlayerState> Players => Game is { Players.IsDefault: false } game ? game.Players : [];
    public string Text(string section, string field) => Panel?.Text(section, field)?.Trim() ?? "";
    public string TextOr(string section, string field, string fallback) => HudView.Text(Text(section, field), fallback);
    public HudPanelReference? Reference(string field) => Panel?.References.TryGetValue("feature", out var fields) == true && fields.TryGetValue(field, out var value) ? value : null;
    public string? ProfileImage(string? steamId) => steamId is not null && Metadata.Matches(Match) && Metadata.Players.TryGetValue(steamId, out var player) ? ResolveMedia(player.Avatar) : null;
    public string Name(PlayerState player) => player.SteamId is not null && Metadata.Matches(Match) && Metadata.Players.TryGetValue(player.SteamId, out var profile) ? HudView.Text(profile.Name, HudView.Text(player.Name)) : HudView.Text(player.Name);
    public IReadOnlyList<PlayerState> TeamPlayers(ManagedTeam team)
    {
        if (Metadata.Matches(Match) && Metadata.Rosters.TryGetValue(team.Id, out var ids) && ids.Count > 0)
            return ids.Select(id => Players.FirstOrDefault(player => player.SteamId == id) ?? new PlayerState(id,
                Metadata.Players.TryGetValue(id, out var profile) ? profile.Name : null, null, null, null)).ToArray();
        var sides = new[] { Match?.Ct, Match?.T }.Where(resolution => Reliable(resolution) && resolution!.TeamId == team.Id).Select(resolution => resolution!.Side).ToArray();
        return Players.Where(player => player.Side is { } side && sides.Contains(side)).ToArray();
    }
    public ManagedTeam? SideTeam(TeamSide side)
    {
        if (Match is not { } match) return null;
        var resolution = side == TeamSide.CounterTerrorists ? match.Ct : match.T;
        if (!Reliable(resolution) || resolution!.Side != side) return null;
        return resolution.TeamId == match.TeamA.Id ? match.TeamA : resolution.TeamId == match.TeamB.Id ? match.TeamB : null;
    }
    public string TeamName(string? id) => id is not null && id == Match?.TeamA.Id ? Match.TeamA.Name : id is not null && id == Match?.TeamB.Id ? Match.TeamB.Name : "Unassigned";
    public IReadOnlyList<string> SponsorImages()
    {
        var images = Match?.Competition?.Sponsors?.Select(sponsor => sponsor.Image) ?? [];
        if (Panel?.Values.TryGetValue("branding", out var fields) == true && fields.TryGetValue("sponsors", out var value) && value.ValueKind == JsonValueKind.Array)
            images = images.Concat(value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!));
        return images.Select(ResolveMedia).Where(image => image is not null).Cast<string>().Distinct(StringComparer.Ordinal).Take(24).ToArray();
    }
    public string StartCountdown => Match?.StartsAt is { } start ? Now < start ? HudView.Time((start - Now).TotalSeconds) : "Scheduled time reached" : "Start time not set";
    public string Competition => Match?.Competition is { } competition ? string.Join(" · ", new[] { competition.Name, competition.SeasonName }.Concat(competition.Sections.Select(section => section.Name)).Where(value => !string.IsNullOrWhiteSpace(value))) : "";
    public string? SeriesWinner
    {
        get
        {
            if (Match is not { } match || match.BestOf is not (1 or 3 or 5 or 7 or 9)) return null;
            var target = match.BestOf / 2 + 1;
            var completed = match.Maps.Where(map => map.Status == MapResultStatus.Completed).ToArray();
            var a = completed.Count(map => map.WinnerTeamId == match.TeamA.Id);
            var b = completed.Count(map => map.WinnerTeamId == match.TeamB.Id);
            if (a != match.TeamA.SeriesWins || b != match.TeamB.SeriesWins) return null;
            return a == target && b < target ? match.TeamA.Name : b == target && a < target ? match.TeamB.Name : null;
        }
    }
    public static bool Reliable(TeamSideResolution? resolution) => resolution?.Status is TeamRecognitionStatus.Recognized or TeamRecognitionStatus.Inferred or TeamRecognitionStatus.Manual;
    public static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "—";
    public static string Decimal(double? value) => value is { } number && double.IsFinite(number) ? number.ToString("0.0", CultureInfo.InvariantCulture) : "—";
    public static string Coverage(RecordedPlayerStats? stats) => stats is null ? "Not recorded" : $"{stats.Rounds} rounds · {(stats.CoversWholeMatch ? "whole match" : "partial coverage")}";
    public static string WeaponName(WeaponState weapon) => HudView.Text(weapon.Name?.StartsWith("weapon_", StringComparison.Ordinal) == true ? weapon.Name[7..] : weapon.Name);
}
