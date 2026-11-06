using System.Globalization;

namespace BasicHud.Presentation;

public sealed record MatchStartView(DateTimeOffset? Planned, DateTimeOffset? Delayed)
{
    public DateTimeOffset? Effective => Delayed ?? Planned;
    public bool IsDelayed => Delayed is not null;
    public bool HasElapsed(DateTimeOffset now) => Effective is { } start && now >= start;
    public string Countdown(DateTimeOffset now) => Effective is { } start
        ? now < start ? HudView.Time((start - now).TotalSeconds) : ""
        : "Start time not set";
    public static string Timestamp(DateTimeOffset value) => value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}
