using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using BasicHud.Presentation;
using SOU.Hud.Sdk;
using SOU.Live.Contracts;

public static class RecordingPushTests
{
    public static async Task Run(Action<bool, string> check)
    {
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        json.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "recording-showcase.json")));
        var baseline = fixture.RootElement.GetProperty("recordingSnapshot").Deserialize<HudRecordingSnapshot>(json)!;
        var selection = new HudRecordingSelection(MapAnalysis: true);
        var selected = HudRecordingProjection.Select(baseline, selection);
        check(!selection.RoundHistory && !selection.EconomyHistory && selected.RoundHistory is null && selected.EconomyHistory is null
            && selected.MapAnalysis is not null, "explicit SDK selections disable unmentioned datasets rather than inventing empty packages");
        HudRecordingState State(string match, string map) => new(match, map, new(true, true, true), Guid.NewGuid(), baseline.Revision,
            baseline.Status, baseline.UpdatedAt, true, baseline.RoundHistory, baseline.MapAnalysis, baseline.EconomyHistory);
        var feed = new FakeFeed();
        using var subscription = new RecordingSubscription(feed);
        var session = Guid.NewGuid();
        subscription.Bind(session, baseline.MatchId, baseline.Map);
        var first = feed.Calls.Single();
        first.Updates.Writer.TryWrite(State(baseline.MatchId, baseline.Map));
        await Until(() => subscription.State is not null);
        var original = subscription.State!;
        subscription.Bind(session, baseline.MatchId, baseline.Map);
        check(feed.Calls.Count == 1, "same recording context shares one subscription across view and panel updates");
        first.Status(HudConnectionStatus.Reconnecting);
        check(subscription.Stale && ReferenceEquals(original, subscription.State), "reconnecting retains the last complete recording and marks it stale");
        first.Status(HudConnectionStatus.Connected);
        check(subscription.Stale, "transport connection alone cannot clear stale data before a synchronized state");
        var fewer = original with { IsSnapshot = true, StreamId = Guid.NewGuid(), MapAnalysis = original.MapAnalysis! with { Players = original.MapAnalysis.Players.Take(1).ToImmutableArray() } };
        first.Updates.Writer.TryWrite(fewer);
        await Until(() => ReferenceEquals(subscription.State, fewer));
        check(!subscription.Stale && subscription.State!.MapAnalysis!.Players.Length == 1, "a new baseline replaces map summaries including lower totals");
        subscription.Bind(session, "new-match", "de_dust2");
        check(first.Token.IsCancellationRequested && subscription.State is null, "match/map switches cancel the previous stream and discard its displayed state");
        first.Updates.Writer.TryWrite(original); first.Status(HudConnectionStatus.Reconnecting);
        var second = feed.Calls[1];
        var next = State("new-match", "de_dust2");
        second.Updates.Writer.TryWrite(next);
        await Until(() => ReferenceEquals(subscription.State, next));
        first.Status(HudConnectionStatus.Reconnecting); first.Updates.Writer.TryWrite(original);
        await Task.Delay(20);
        check(ReferenceEquals(subscription.State, next) && !subscription.Stale, "late states and connection callbacks from obsolete subscriptions cannot overwrite the new context");
        second.Updates.Writer.TryWrite(original);
        await Task.Delay(20);
        check(ReferenceEquals(subscription.State, next), "mismatched push targets are rejected even on the active subscription");
        subscription.Bind(session, "new-match", "de_dust2", restart: true);
        check(second.Token.IsCancellationRequested && feed.Calls.Count == 3 && subscription.Stale, "manual synchronization replaces the subscription without a data GET or a timer");
        subscription.Dispose();
        check(feed.Calls[2].Token.IsCancellationRequested, "closing recording views releases their subscription");
        foreach (var call in feed.Calls) call.Updates.Writer.TryComplete();
    }

    private static async Task Until(Func<bool> test)
    {
        for (var i = 0; !test(); i++) { if (i > 1000) throw new Exception("Push test timed out"); await Task.Delay(1); }
    }
    private sealed class FakeFeed : IRecordingFeed
    {
        public sealed record Call(Action<HudConnectionStatus> Status, CancellationToken Token, Channel<HudRecordingState> Updates);
        public List<Call> Calls { get; } = [];
        public async IAsyncEnumerable<HudRecordingState> Watch(string match, string map, Action<HudConnectionStatus> status, [EnumeratorCancellation] CancellationToken token)
        {
            var call = new Call(status, token, Channel.CreateUnbounded<HudRecordingState>()); Calls.Add(call);
            // Deliberately ignore cancellation here to test late SDK completions.
            await foreach (var update in call.Updates.Reader.ReadAllAsync()) yield return update;
        }
    }
}
