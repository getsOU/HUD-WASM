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
    public event Action? Changed;
    public HudView View { get; private set; } = HudView.Empty;
    public DisplayOptions Options { get; private set; } = DisplayOptions.Default;
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
                sessionId = snapshot.SessionId;
                revision = snapshot.Revision;
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
                if (message.State is not { } state || state.Revision < panelRevision) continue;
                panelRevision = state.Revision;
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
