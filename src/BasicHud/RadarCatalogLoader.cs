using System.Text.Json;
using System.Text.Json.Serialization;
using BasicHud.Core;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace BasicHud;

internal static class RadarCatalogLoader
{
    public static async Task<RadarCatalog> LoadAsync(HttpClient http, Uri baseAddress, CancellationToken stop)
    {
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseAddress, "assets/radar/catalog.json"));
            if (OperatingSystem.IsBrowser())
            {
                request.SetBrowserRequestMode(BrowserRequestMode.SameOrigin);
                request.SetBrowserRequestOption("redirect", "error");
            }
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri is { } final && Uri.Compare(final, baseAddress, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) != 0) return RadarCatalog.Empty;
            const int limit = 256 * 1024;
            if (response.Content.Headers.ContentLength > limit) return RadarCatalog.Empty;
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var buffer = new MemoryStream();
            var bytes = new byte[4096];
            int length;
            while ((length = await stream.ReadAsync(bytes, deadline.Token)) > 0)
            {
                if (buffer.Length + length > limit) return RadarCatalog.Empty;
                buffer.Write(bytes, 0, length);
            }
            return RadarCatalog.Bind(JsonSerializer.Deserialize(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)), RadarCatalogJson.Default.RadarCatalogDocument));
        }
        catch (Exception error) when (error is HttpRequestException or IOException or JsonException or OperationCanceledException)
        { return RadarCatalog.Empty; }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, MaxDepth = 16)]
[JsonSerializable(typeof(RadarCatalogDocument))]
internal partial class RadarCatalogJson : JsonSerializerContext;
