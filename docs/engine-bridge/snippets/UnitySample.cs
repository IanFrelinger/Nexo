// Starter: Unity / .NET host calling Ashlar.API Forge endpoints.
// Add assembly references to System.Net.Http / System.Text.Json as needed.

using System.Net.Http;
using System.Text.Json;

/// <summary>Minimal loader — replace URLs with your Ashlar.API base.</summary>
public static class AshlarForgeBridge
{
    public static async Task<string?> FetchManifestJsonAsync(HttpClient http, string baseUrl, string engineId)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/forge/engine/{Uri.EscapeDataString(engineId)}/aesthetic-manifest";
        using var resp = await http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("json").GetString();
    }

    public static async Task<JsonDocument?> FetchMaterialHintsAsync(HttpClient http, string baseUrl, string parseKind)
    {
        var url =
            $"{baseUrl.TrimEnd('/')}/api/forge/map/material-hints?parseKind={Uri.EscapeDataString(parseKind)}";
        using var resp = await http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    }
}
