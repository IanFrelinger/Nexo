// Portability spike: external product client that probes a hosted brick via Ashlar.Sdk.Client.
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Client;
using Ashlar.Sdk.Client;

if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: ExternalProductClient <hostBaseUrl>");
    Environment.Exit(2);
}

var hostBaseUrl = args[0].TrimEnd('/');
var services = new ServiceCollection();
services.AddAshlarClientSdk(hostBaseUrl);
await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IAshlarClient>();

const string probeLogText =
    "2024-01-01 INFO Started\n" +
    "2024-01-01 ERROR First failure: connection reset\n" +
    "2024-01-01 WARN Retrying\n" +
    "2024-01-01 ERROR Second failure: timeout";

var requestBody = new Dictionary<string, object?>
{
    ["implementation"] = "Deterministic",
    ["input"] = new Dictionary<string, object> { ["logText"] = probeLogText }
};

var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
using var content = JsonContent.Create(requestBody, options: jsonOptions);

using var response = await client.InvokeAsync(
    HttpMethod.Post,
    "api/bricks/error-summary-extractor/execute",
    content).ConfigureAwait(false);

response.EnsureSuccessStatusCode();
await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

if (!doc.RootElement.TryGetProperty("success", out var successEl) || !successEl.GetBoolean())
{
    var error = doc.RootElement.TryGetProperty("error", out var errEl) ? errEl.GetString() : "unknown";
    Console.Error.WriteLine($"Brick execute failed: {error}");
    Environment.Exit(1);
}

if (!doc.RootElement.TryGetProperty("output", out var outputEl) ||
    !outputEl.TryGetProperty("errorCount", out var countEl) ||
    countEl.ValueKind != JsonValueKind.Number ||
    countEl.GetInt32() != 2 ||
    !outputEl.TryGetProperty("firstErrorMessage", out var msgEl) ||
    msgEl.ValueKind != JsonValueKind.String ||
    msgEl.GetString() != "First failure: connection reset")
{
    Console.Error.WriteLine($"Unexpected brick output: {doc.RootElement.GetRawText()}");
    Environment.Exit(1);
}

Console.WriteLine("external-product-client: probe brick round-trip OK (errorCount=2, first='First failure: connection reset')");
