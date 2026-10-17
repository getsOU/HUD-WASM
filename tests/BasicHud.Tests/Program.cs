using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using BasicHud.Core;
using SOU.Live.Contracts;

var checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    Console.WriteLine("PASS " + description);
    checks++;
}
var id = Guid.NewGuid();
LiveSnapshot Snapshot(GameState? game, long revision = 1, Guid? session = null) => new(2, session ?? id, revision, null, DataFreshness.Live, game);
GameState Game(ImmutableArray<PlayerState> players = default) => new(null, null, null, null, null, null, null, players);
ManagedMatch Match(TeamSideResolution ct, TeamSideResolution t) => new("match", "Match", 3, new("A", "Alpha", "A", null, 0), new("B", "Bravo", "B", null, 0), ct, t);
var ct = new TeamSideResolution(TeamSide.CounterTerrorists, TeamRecognitionStatus.Recognized, "B", 0, 0);
var t = new TeamSideResolution(TeamSide.Terrorists, TeamRecognitionStatus.Manual, "A", 0, 0);
var mapped = Snapshot(Game()) with { ManagedMatch = Match(ct, t) };
var view = HudView.From(mapped);
Check(view.Ct.Name == "Bravo" && view.T.Name == "Alpha", "managed identity follows explicit side resolution rather than TeamA/B order");
Check(HudView.From(mapped with { ManagedMatch = Match(ct with { Status = TeamRecognitionStatus.Conflicting }, t) }).Ct.Name == "CT", "conflicting mapping does not invent a managed team");
Check(HudView.From(mapped with { ManagedMatch = Match(ct with { TeamId = "b" }, t) }).Ct.Name == "CT", "opaque team IDs are case-sensitive");
Check(HudView.From(mapped with { ManagedMatch = Match(ct with { Side = TeamSide.Terrorists }, t) }).Ct.Name == "CT", "incorrect side resolution is rejected");
Check(HudView.From(Snapshot(null) with { ManagedMatch = mapped.ManagedMatch }).Ct.Name == "Bravo", "managed match can be displayed before first GSI");
Check(view.Ct.Score == "—" && view.Ct.Players.Count == 0 && view.Observed is null && view.Round == "ROUND —", "missing values never turn into zero statistics");
Check(HudView.Time(double.NaN) == "—" && HudView.Time(double.PositiveInfinity) == "—" && HudView.Time(-1) == "—", "invalid source countdowns remain unknown");
Check(HudView.Time(61.2) == "1:02" && HudView.Time(0) == "0:00", "timer rounds source seconds upward without extrapolation");
var bomb = HudView.From(Snapshot(Game() with { Bomb = new(BombStatus.Planted, null, null, null), Countdown = new(CountdownPhase.Live, 91) }));
Check(bomb.Timer.IsBomb && bomb.Timer.Value == "—", "missing bomb timer does not reuse unrelated round time");
var defused = HudView.From(Snapshot(Game() with { Bomb = new(BombStatus.Defused, null, null, 0), Countdown = new(CountdownPhase.Live, 91) }));
var exploded = HudView.From(Snapshot(Game() with { Bomb = new(BombStatus.Exploded, null, null, null) }));
Check(defused.Timer.Label == "BOMB DEFUSED" && defused.Timer.Value == "0:00" && exploded.Timer.Label == "BOMB EXPLODED" && exploded.Timer.Value == "—", "terminal bomb states show only their own source seconds, never round time");
var slots = new[] { 0, 9, 1, -1 }.Select(slot => new PlayerState(slot.ToString(), "Player", TeamSide.CounterTerrorists, null, null) { ObserverSlot = slot }).ToImmutableArray();
Check(HudView.From(Snapshot(Game(slots))).Ct.Players.Select(player => player.Slot).SequenceEqual(["1", "9", "0", "—"]), "observer digits sort 1..9 then zero, unknown last");
var unknownHp = HudView.From(Snapshot(Game([new("1", null, TeamSide.Terrorists, null, null)]))).T.Players[0];
Check(!unknownHp.IsDead && unknownHp.Health == "—" && unknownHp.HealthPercent is null, "unknown health is not treated as death");
var reloading = new PlayerState("1", "Reloading", TeamSide.CounterTerrorists, null, 70) { Weapons = [new("0", "weapon_m4a1", null, WeaponStatus.Reloading, null, 4, null, 30)] };
Check(HudView.From(Snapshot(Game([reloading]) with { ObservedPlayer = reloading })).Observed?.Ammo == "4 / 30", "reloading weapon remains visible with source ammunition");
var stats = reloading with { Stats = new(17, 3, 8, null, null) };
Check(HudView.From(Snapshot(Game([stats]))).Ct.Players[0].Kda == "17 / 8 / 3", "statistics display kills / deaths / assists consistently");
HudPanelState Panel(string json) => new(1, JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(json)!);
Check(DisplayOptions.FromPanel(Panel("{}")) == DisplayOptions.Default, "missing panel values retain visible defaults");
Check(DisplayOptions.FromPanel(Panel("{\"display\":{\"hideRosters\":\"true\",\"caption\":\"  \"}}")) == DisplayOptions.Default, "malformed checkbox and blank caption use safe defaults");
var schema = JsonSerializer.Deserialize<HudPanelSection[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "panel.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
var fields = schema.Single(section => section.Name == "display").Inputs;
Check(fields.Where(field => field.Type == HudPanelFieldType.Checkbox).Select(field => field.Name).Order().SequenceEqual(new[] { "hideEconomy", "hideKillfeed", "hideObservedPlayer", "hideRadar", "hideRosters", "hideRoundHistory", "hideSeries", "hideSponsors" }), "shipped schema uses opt-out visibility fields for all components");
// Model the documented host baseline from the actual shipped schema, rather than an empty
// client-only state: the host sends false for every unset checkbox, including revision zero.
HudPanelState HostDefault(long revision) => new(revision, schema.ToDictionary(section => section.Name,
    section => section.Inputs.Where(field => field.Type != HudPanelFieldType.Action).ToDictionary(field => field.Name,
        field => field.Type == HudPanelFieldType.Checkbox ? JsonSerializer.SerializeToElement(false) : JsonSerializer.SerializeToElement(""))));
Check(DisplayOptions.FromPanel(HostDefault(0)) == DisplayOptions.Default, "host-normalized false checkboxes show both sections at first load");
var captionOnly = HostDefault(1);
captionOnly.Values["display"]["caption"] = JsonSerializer.SerializeToElement("My cast");
Check(DisplayOptions.FromPanel(captionOnly) == new DisplayOptions("My cast", true, true), "saving only a caption keeps both sections visible at revision one");
var rosterHidden = HostDefault(2);
rosterHidden.Values["display"]["hideRosters"] = JsonSerializer.SerializeToElement(true);
Check(DisplayOptions.FromPanel(rosterHidden) == new DisplayOptions("sOU BASIC", false, true), "true hides only rosters without hiding the observed player");
var observedHidden = HostDefault(3);
observedHidden.Values["display"]["hideObservedPlayer"] = JsonSerializer.SerializeToElement(true);
Check(DisplayOptions.FromPanel(observedHidden) == new DisplayOptions("sOU BASIC", true, false), "true hides only the observed player without hiding rosters");
rosterHidden.Values["display"]["hideRosters"] = JsonSerializer.SerializeToElement(false);
observedHidden.Values["display"]["hideObservedPlayer"] = JsonSerializer.SerializeToElement(false);
Check(DisplayOptions.FromPanel(rosterHidden) == DisplayOptions.Default && DisplayOptions.FromPanel(observedHidden) == DisplayOptions.Default,
    "clearing each Hide checkbox shows its section again without revision special cases");

var feed = new TestFeed();
var hud = new HudSession(feed);
hud.Start();
await feed.Live.Writer.WriteAsync(mapped with { Revision = 20 });
await Until(() => hud.View.Ct.Name == "Bravo");
await feed.Live.Writer.WriteAsync(Snapshot(null, 19));
await feed.Panel.Writer.WriteAsync(new("sou-basic-wasm", "0.1.0", Panel("{\"display\":{\"caption\":\"First\"}}") with { Revision = 50 }));
await Until(() => hud.Options.Caption == "First");
Check(hud.View.Ct.Name == "Bravo", "out-of-order snapshots within a session are ignored");
await feed.Live.Writer.WriteAsync(Snapshot(null, 1, Guid.NewGuid()));
await Until(() => !hud.View.HasGame);
Check(hud.View.Ct.Players.Count == 0 && hud.View.Observed is null && hud.View.Ct.Score == "—", "a new session clears old players, scores and observed player");
feed.Emit("panel", ConnectionState.Reconnecting);
await feed.Panel.Writer.WriteAsync(new("sou-basic-wasm", "0.1.0", Panel("{\"display\":{\"caption\":\"Restarted\"}}") with { Revision = 1 }));
await Until(() => hud.Options.Caption == "Restarted");
Check(hud.Options.Caption == "Restarted", "panel numbering can restart after host reconnect");
await hud.DisposeAsync();
await hud.DisposeAsync();
Check(feed.ActiveReaders == 0 && feed.SubscriberCount == 0, "disposal cancels and awaits both streams and unsubscribes events idempotently");
var broken = new TestFeed();
await using (var brokenHud = new HudSession(broken))
{
    brokenHud.Start();
    broken.Live.Writer.Complete(new InvalidDataException("synthetic failure"));
    await Until(() => brokenHud.LiveConnection == ConnectionState.Failed);
    Check(brokenHud.LiveConnection == ConnectionState.Failed, "unexpected feed failures become a visible connection failure");
}
await ShowcaseTests.Run(Check);
await RenderTests.Run(Check);
await CompactTests.Run(Check);
Console.WriteLine($"{checks} checks passed.");

static async Task Until(Func<bool> ready)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    while (!ready()) await Task.Delay(10, timeout.Token);
}

sealed class TestFeed : IHudFeed
{
    private Action<string, ConnectionState>? changed;
    public event Action<string, ConnectionState>? ConnectionChanged { add => changed += value; remove => changed -= value; }
    public int SubscriberCount => changed?.GetInvocationList().Length ?? 0;
    public int ActiveReaders;
    public Channel<LiveSnapshot> Live { get; } = Channel.CreateUnbounded<LiveSnapshot>();
    public Channel<HudPanelMessage> Panel { get; } = Channel.CreateUnbounded<HudPanelMessage>();
    public void Emit(string channel, ConnectionState state) => changed?.Invoke(channel, state);
    public IAsyncEnumerable<LiveSnapshot> WatchLiveAsync(CancellationToken token) => Read(Live, token);
    public IAsyncEnumerable<HudPanelMessage> WatchPanelAsync(CancellationToken token) => Read(Panel, token);
    private async IAsyncEnumerable<T> Read<T>(Channel<T> channel, [EnumeratorCancellation] CancellationToken token)
    {
        Interlocked.Increment(ref ActiveReaders);
        try { await foreach (var message in channel.Reader.ReadAllAsync(token)) yield return message; }
        finally { Interlocked.Decrement(ref ActiveReaders); }
    }
}
