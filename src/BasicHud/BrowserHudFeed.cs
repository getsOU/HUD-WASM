using BasicHud.Core;
using SOU.Hud.Sdk;
using SOU.Live.Contracts;

namespace BasicHud;

internal sealed class BrowserHudFeed : IHudFeed, IDisposable
{
    private readonly HudClient client;
    public event Action<string, ConnectionState>? ConnectionChanged;

    public BrowserHudFeed(HttpClient http, Uri address)
    {
        client = new HudClient(http, address);
        client.ConnectionChanged += OnConnectionChanged;
    }

    public IAsyncEnumerable<LiveSnapshot> WatchLiveAsync(CancellationToken cancellationToken) => client.WatchAsync(cancellationToken);
    public IAsyncEnumerable<HudPanelMessage> WatchPanelAsync(CancellationToken cancellationToken) => client.WatchPanelAsync(cancellationToken);
    public string? Media(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try { return client.Media(path).AbsoluteUri; }
        catch (ArgumentException) { return null; }
    }
    private void OnConnectionChanged(string channel, HudConnectionStatus status) => ConnectionChanged?.Invoke(channel, status switch
    {
        HudConnectionStatus.Connected => ConnectionState.Connected,
        HudConnectionStatus.Reconnecting => ConnectionState.Reconnecting,
        HudConnectionStatus.Stopped => ConnectionState.Stopped,
        _ => ConnectionState.Connecting
    });
    public void Dispose() => client.ConnectionChanged -= OnConnectionChanged;
}
