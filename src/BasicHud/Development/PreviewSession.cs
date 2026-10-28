using BasicHud.Presentation;
using SOU.Live.Contracts;

namespace BasicHud.Development;

public enum PreviewConnectionState { Connecting, Connected, Reconnecting, Stopped, Failed }

public interface IPreviewFeed
{
    event Action<string, PreviewConnectionState>? ConnectionChanged;
    IAsyncEnumerable<LiveSnapshot> WatchLiveAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<HudPanelMessage> WatchPanelAsync(CancellationToken cancellationToken);
}

/// <summary>Debug-only synthetic feed lifecycle. Production sessions are owned by the sOU SDK.</summary>
internal sealed class PreviewSession(IPreviewFeed feed) : IAsyncDisposable
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
    public PreviewConnectionState LiveConnection { get; private set; } = PreviewConnectionState.Connecting;
    public PreviewConnectionState PanelConnection { get; private set; } = PreviewConnectionState.Connecting;

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
        catch (Exception) { LiveConnection = PreviewConnectionState.Failed; Notify(); }
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
        catch (Exception) { PanelConnection = PreviewConnectionState.Failed; Notify(); }
    }

    private void ConnectionChanged(string channel, PreviewConnectionState state)
    {
        if (stop.IsCancellationRequested) return;
        if (channel == "live") LiveConnection = state;
        if (channel == "panel")
        {
            PanelConnection = state;
            // A restarted host may restart panel revision numbering.
            if (state is PreviewConnectionState.Connecting or PreviewConnectionState.Reconnecting) panelRevision = long.MinValue;
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
