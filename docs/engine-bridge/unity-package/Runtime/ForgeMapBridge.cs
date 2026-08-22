// Unity Package Manager layout — copy Runtime into Assets or add via git URL / local path.
// Requires Unity scripting backend with System.Net.Http + System.Text.Json (modern Unity).

using System.Net.Http;
using System.Text.Json;

namespace Ashlar.ForgeMapBridge
{
    /// <summary>Minimal HTTP helpers for Ashlar Forge endpoints.</summary>
    public static class ForgeMapBridge
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
}
