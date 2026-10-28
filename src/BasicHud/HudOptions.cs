using System.Collections.Immutable;
using System.Text.Json.Serialization;
using BasicHud.Presentation;

namespace BasicHud;

public sealed record HudOptions(string Caption = "", ImmutableDictionary<string, HudPlayerProfile>? Players = null, HudMatchProfiles? Match = null)
{
    public BrowserMetadata Metadata() => new()
    {
        Players = Players?.Where(pair => pair.Value is not null).ToDictionary(pair => pair.Key,
            pair => new BrowserPlayer { Name = pair.Value.Name, Avatar = pair.Value.Avatar }, StringComparer.Ordinal),
        Match = Match is null ? null : new() { Left = Match.Left?.Metadata(), Right = Match.Right?.Metadata() }
    };
}
public sealed record HudPlayerProfile(string? Name = null, string? Avatar = null);
public sealed record HudMatchProfiles(HudTeamProfile? Left = null, HudTeamProfile? Right = null);
public sealed record HudTeamProfile(string? Id = null, ImmutableArray<string>? SteamIds = null)
{
    public BrowserTeam Metadata() => new() { Id = Id, SteamIds = SteamIds is { IsDefault: false } ids ? ids.ToList() : null };
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(HudOptions))]
internal partial class HudOptionsJson : JsonSerializerContext;
