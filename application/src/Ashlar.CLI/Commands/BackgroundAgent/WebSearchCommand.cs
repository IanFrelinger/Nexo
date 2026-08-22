using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ashlar.BackgroundAgents.WebSearch;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// CLI command handler for web search (configure, test).
/// </summary>
public class WebSearchCommand
{
    private readonly IWebSearchProvider? _provider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebSearchCommand> _logger;

    /// <summary>Creates a new WebSearchCommand instance.</summary>
    public WebSearchCommand(
        IConfiguration configuration,
        ILogger<WebSearchCommand> logger,
        IWebSearchProvider? provider = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _provider = provider;
    }

    /// <summary>
    /// Show web search configuration (env keys and hints).
    /// </summary>
    public Task<int> ConfigureAsync(bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var provider = _provider?.ProviderId ?? "none";
            var bingKey = _configuration["BackgroundAgents:WebSearch:ApiKey"]
                ?? _configuration["BING_SEARCH_KEY"]
                ?? _configuration["WebSearch:ApiKey"]
                ?? "(not set)";
            if (bingKey == "(not set)" && Environment.GetEnvironmentVariable("BING_SEARCH_KEY") != null)
                bingKey = "(set via env)";

            if (formatJson)
            {
                var payload = new
                {
                    ok = true,
                    provider,
                    apiKeyConfigured = bingKey != "(not set)" && bingKey != "(set via env)" ? "yes" : "no",
                    hint = "Set BING_SEARCH_KEY or BackgroundAgents:WebSearch:ApiKey for Bing; register IWebSearchProvider for test."
                };
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(payload, options));
            }
            else
            {
                Console.Out.WriteLine("Web Search Configuration:");
                Console.Out.WriteLine($"  Provider: {provider}");
                Console.Out.WriteLine($"  ApiKey: {bingKey}");
                Console.Out.WriteLine("  Hint: Set BING_SEARCH_KEY or BackgroundAgents:WebSearch:ApiKey for Bing; register IWebSearchProvider for test.");
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web search configure failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// Run a test search (uses registered IWebSearchProvider).
    /// </summary>
    public async Task<int> TestAsync(string query, int maxResults, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            if (_provider == null)
            {
                if (formatJson)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "No web search provider registered" }));
                else
                    Console.Error.WriteLine("No web search provider registered. Register IWebSearchProvider in DI to run test.");
                return 1;
            }
            if (string.IsNullOrWhiteSpace(query))
                query = "Ashlar framework";

            var results = await _provider.SearchAsync(query, maxResults, ct).ConfigureAwait(false);
            if (formatJson)
            {
                var items = results.Select(r => new { r.Title, r.Url, r.Snippet }).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, query, provider = _provider.ProviderId, results = items }, options));
            }
            else
            {
                Console.Out.WriteLine($"Web Search Test: \"{query}\" ({_provider.ProviderId})");
                foreach (var r in results)
                {
                    Console.Out.WriteLine($"  {r.Title}");
                    Console.Out.WriteLine($"    {r.Url}");
                    Console.Out.WriteLine($"    {r.Snippet}");
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web search test failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
