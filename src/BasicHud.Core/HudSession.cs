using SOU.Live.Contracts;

namespace BasicHud.Core;

public enum ConnectionState { Connecting, Connected, Reconnecting, Stopped, Failed }

public interface IHudFeed
{
    event Action<string, ConnectionState>? ConnectionChanged;
    IAsyncEnumerable<LiveSnapshot> WatchLiveAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<HudPanelMessage> WatchPanelAsync(CancellationToken cancellationToken);
}

/// <summary>Owns both subscriptions and ignores stale revisions within a session.</summary>
public sealed class HudSession(IHudFeed feed) : IAsyncDisposable
{
    private readonly CancellationTokenSource stop = new();
    private Task work = Task.CompletedTask;
    private Guid? sessionId;
    private long revision = long.MinValue;
    private long panelRevision = long.MinValue;
    private bool started;
    private bool disposed;
    private readonly HashSet<Guid> actionIds = [];
    private readonly Queue<Guid> actionOrder = [];
    public event Action? Changed;
    public HudView View { get; private set; } = HudView.Empty;
    public DisplayOptions Options { get; private set; } = DisplayOptions.Default;
    public LiveSnapshot? Snapshot { get; private set; }
    public HudPanelState? Panel { get; private set; }
    public bool LowerThirdVisible { get; private set; }
    public ConnectionState LiveConnection { get; private set; } = ConnectionState.Connecting;
    public ConnectionState PanelConnection { get; private set; } = ConnectionState.Connecting;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (started) throw new InvalidOperationException("The HUD session has already started.");
        started = true;
        feed.ConnectionChanged += ConnectionChanged;
        work = Task.WhenAll(ReadLiveAsync(), ReadPanelAsync());
    }

    private async Task ReadLiveAsync()
    {
        try
        {
            await foreach (var snapshot in feed.WatchLiveAsync(stop.Token))
            {
                if (stop.IsCancellationRequested) break;
                if (sessionId == snapshot.SessionId && snapshot.Revision <= revision) continue;
                if (sessionId != snapshot.SessionId) { LowerThirdVisible = false; actionIds.Clear(); actionOrder.Clear(); }
                sessionId = snapshot.SessionId;
                revision = snapshot.Revision;
                Snapshot = snapshot;
                View = HudView.From(snapshot); // Replaces the entire view, including empty/new sessions.
                Notify();
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (Exception) { LiveConnection = ConnectionState.Failed; Notify(); }
    }

    private async Task ReadPanelAsync()
    {
        try
        {
            await foreach (var message in feed.WatchPanelAsync(stop.Token))
            {
                if (stop.IsCancellationRequested) break;
                if (message.Action is { Section: "production", Name: "lowerThird", Value: "show" or "hide" } action && actionIds.Add(action.Id))
                {
                    actionOrder.Enqueue(action.Id);
                    if (actionOrder.Count > 1024) actionIds.Remove(actionOrder.Dequeue());
                    LowerThirdVisible = action.Value == "show";
                    Notify();
                }
                if (message.State is not { } state || state.Revision < panelRevision) continue;
                panelRevision = state.Revision;
                Panel = state;
                Options = DisplayOptions.FromPanel(state);
                Notify();
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (Exception) { PanelConnection = ConnectionState.Failed; Notify(); }
    }

    private void ConnectionChanged(string channel, ConnectionState state)
    {
        if (stop.IsCancellationRequested) return;
        if (channel == "live") LiveConnection = state;
        if (channel == "panel")
        {
            PanelConnection = state;
            // A restarted host may restart panel revision numbering.
            if (state is ConnectionState.Connecting or ConnectionState.Reconnecting) panelRevision = long.MinValue;
        }
        Notify();
    }
    private void Notify() { if (!disposed) Changed?.Invoke(); }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        feed.ConnectionChanged -= ConnectionChanged;
        await stop.CancelAsync();
        await work;
        Changed = null;
        stop.Dispose();
    }
}
