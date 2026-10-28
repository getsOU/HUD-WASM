using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using BasicHud.Core;
using SOU.Live.Contracts;

namespace BasicHud;

// Retained for bounded v2 metadata regression checks; production v3 uses session options.
internal sealed class MetadataPoller(HttpClient http, Uri packageAddress)
{
    public async Task RunAsync(Func<LiveSnapshot?> current, Action<MetadataCatalog, Guid?> update, CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            var before = current();
            var match = before?.ManagedMatch ?? before?.Game?.ManagedMatch;
            if (match is not null)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
                    timeout.CancelAfter(TimeSpan.FromSeconds(3));
                    using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(packageAddress, "/hud-browser/options"));
                    if (OperatingSystem.IsBrowser())
                    {
                        request.SetBrowserRequestMode(BrowserRequestMode.SameOrigin);
                        request.SetBrowserRequestCredentials(BrowserRequestCredentials.SameOrigin);
                        request.SetBrowserRequestOption("redirect", "error");
                    }
                    using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                    response.EnsureSuccessStatusCode();
                    if (response.RequestMessage?.RequestUri is { } final && Uri.Compare(final, packageAddress, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) != 0)
                        throw new InvalidDataException("Metadata must remain on the package origin.");
                    const int limit = 1024 * 1024;
                    if (response.Content.Headers.ContentLength is > limit) throw new InvalidDataException("Metadata response too large.");
                    await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                    using var buffer = new MemoryStream();
                    var bytes = new byte[8192];
                    int count;
                    while ((count = await stream.ReadAsync(bytes, timeout.Token)) > 0)
                    {
                        if (buffer.Length + count > limit) throw new InvalidDataException("Metadata response too large.");
                        buffer.Write(bytes, 0, count);
                    }
                    var data = JsonSerializer.Deserialize(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)), MetadataJson.Default.BrowserMetadata);
                    var after = current();
                    var afterMatch = after?.ManagedMatch ?? after?.Game?.ManagedMatch;
                    update(before?.SessionId == after?.SessionId && afterMatch is not null && match.Id == afterMatch.Id && match.TeamA.Id == afterMatch.TeamA.Id && match.TeamB.Id == afterMatch.TeamB.Id
                        ? MetadataCatalog.Bind(data, afterMatch) : MetadataCatalog.Empty, after?.SessionId);
                }
                catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException or JsonException or OperationCanceledException)
                { if (!stop.IsCancellationRequested) update(MetadataCatalog.Empty, before?.SessionId); }
            }
            else update(MetadataCatalog.Empty, before?.SessionId);
            try { await Task.Delay(TimeSpan.FromSeconds(2), stop); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { break; }
        }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, MaxDepth = 32)]
[JsonSerializable(typeof(BrowserMetadata))]
internal partial class MetadataJson : JsonSerializerContext;
