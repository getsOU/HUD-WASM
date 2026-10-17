using System.Text.Json;
using System.Text.Json.Serialization;
using BasicHud.Core;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace BasicHud;

internal static class RadarCatalogLoader
{
    public static async Task<RadarCatalog> LoadAsync(HttpClient http, Uri baseAddress, CancellationToken stop) =>
        RadarCatalog.Bind(await LoadDocumentAsync(http, baseAddress, "assets/radar/catalog.json", RadarCatalogJson.Default.RadarCatalogDocument, stop));

    public static async Task<BoltRadarCatalog> LoadBoltAsync(HttpClient http, Uri baseAddress, CancellationToken stop) =>
        BoltRadarCatalog.Bind(await LoadDocumentAsync(http, baseAddress, "assets/boltobserv/catalog.json", RadarCatalogJson.Default.BoltRadarDocument, stop));

    private static async Task<T?> LoadDocumentAsync<T>(HttpClient http, Uri baseAddress, string path, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type, CancellationToken stop)
    {
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseAddress, path));
            if (OperatingSystem.IsBrowser())
            {
                request.SetBrowserRequestMode(BrowserRequestMode.SameOrigin);
                request.SetBrowserRequestOption("redirect", "error");
            }
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri is { } final && Uri.Compare(final, baseAddress, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) != 0) return default;
            const int limit = 256 * 1024;
            if (response.Content.Headers.ContentLength > limit) return default;
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var buffer = new MemoryStream();
            var bytes = new byte[4096];
            int length;
            while ((length = await stream.ReadAsync(bytes, deadline.Token)) > 0)
            {
                if (buffer.Length + length > limit) return default;
                buffer.Write(bytes, 0, length);
            }
            return JsonSerializer.Deserialize(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)), type);
        }
        catch (Exception error) when (error is HttpRequestException or IOException or JsonException or OperationCanceledException)
        { return default; }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, MaxDepth = 16)]
[JsonSerializable(typeof(RadarCatalogDocument))]
[JsonSerializable(typeof(BoltRadarDocument))]
internal partial class RadarCatalogJson : JsonSerializerContext;
