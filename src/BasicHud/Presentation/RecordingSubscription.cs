using System.Runtime.CompilerServices;
using SOU.Hud.Sdk;
using SOU.Live.Contracts;

namespace BasicHud.Presentation;

public interface IRecordingFeed
{
    IAsyncEnumerable<HudRecordingState> Watch(string match, string map, Action<HudConnectionStatus> status, CancellationToken token);
}

public sealed class RecordingFeed(HudClient client) : IRecordingFeed
{
    public async IAsyncEnumerable<HudRecordingState> Watch(string match, string map, Action<HudConnectionStatus> status,
        [EnumeratorCancellation] CancellationToken token)
    {
        void Connection(string channel, HudConnectionStatus value) { if (channel == "recording") status(value); }
        client.ConnectionChanged += Connection;
        try
        {
            // All six related views share all three selected datasets. The SDK alone
            // assembles bounded chunks, replaces rounds and resynchronizes baselines.
            await foreach (var state in client.WatchRecordingAsync(match, map, selection: null, token)) yield return state;
        }
        finally { client.ConnectionChanged -= Connection; }
    }
}

/// <summary>One lifetime for the recording view family, fenced against obsolete subscriptions.</summary>
public sealed class RecordingSubscription(IRecordingFeed feed) : IDisposable
{
    public HudRecordingState? State { get; private set; }
    public bool Stale { get; private set; }
    public string Status { get; private set; } = "waiting";
    public event Action? Changed;
    private (Guid? Session, string Match, string Map)? target;
    private CancellationTokenSource? pending;
    private long generation;
    private bool disposed;

    public void Bind(Guid? session, string? match, string? map, bool restart = false)
    {
        var next = !string.IsNullOrWhiteSpace(match) && !string.IsNullOrWhiteSpace(map) ? (session, match, map) : ((Guid?, string, string)?)null;
        if (disposed || !restart && next == target) return;
        var same = next == target;
        target = next;
        ++generation; pending?.Cancel(); pending?.Dispose(); pending = null;
        if (!same) State = null;
        Stale = State is not null;
        Status = next is null ? "waiting" : "loading";
        Changed?.Invoke();
        if (next is not { } chosen) return;
        pending = new();
        _ = Run(chosen.Item2, chosen.Item3, generation, pending.Token);
    }

    private async Task Run(string match, string map, long request, CancellationToken token)
    {
        bool Current() => !disposed && request == generation && !token.IsCancellationRequested;
        try
        {
            await foreach (var state in feed.Watch(match, map, status =>
            {
                if (!Current() || status != HudConnectionStatus.Reconnecting) return;
                Stale = State is not null; Status = "reconnecting"; Changed?.Invoke();
            }, token))
            {
                if (!Current() || state.MatchId != match || state.Map != map) continue;
                // Complete SDK states replace earlier data, including lower totals.
                State = state; Stale = false; Status = state.Status.ToString().ToLowerInvariant(); Changed?.Invoke();
            }
            if (Current()) { Stale = State is not null; Status = "error"; Changed?.Invoke(); }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception) when (Current()) { Stale = State is not null; Status = "error"; Changed?.Invoke(); }
        catch (Exception) when (!Current()) { /* Obsolete stream failures cannot affect the current view. */ }
    }

    public void Dispose()
    {
        disposed = true; ++generation; pending?.Cancel(); pending?.Dispose(); pending = null; Changed = null;
    }
}
