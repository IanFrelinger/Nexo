using Ashlar.Agents.TestKit;
using System.Net;
using System.Text;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.DataSensitivity;
using Ashlar.BackgroundAgents.Metrics;
using Ashlar.BackgroundAgents.Observation;
using Ashlar.BackgroundAgents.WebSearch;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Trust.Ports;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents;

/// <summary>Tests for background agents gap coverage.</summary>
public class BackgroundAgentsGapCoverageTests
{
    [Fact]
    public async Task NoApprovalGate_always_denies()
    {
        var gate = new NoApprovalGate();
        var result = await gate.RequestApprovalAsync("action", TimeSpan.FromSeconds(1));
        result.Should().Be(ApprovalResult.Denied);
    }

    [Fact]
    public void WebSearchConfig_and_result_round_trip()
    {
        var cfg = new WebSearchConfig { Enabled = true, MaxResults = 5, SearchProvider = "bing", ApiKey = "key" };
        cfg.Enabled.Should().BeTrue();
        cfg.MaxResults.Should().Be(5);
        cfg.SearchProvider.Should().Be("bing");

        cfg.FilterSensitiveContent.Should().BeTrue();
        cfg.AllowedDomains = ["example.com"];
        cfg.BlockedDomains = ["spam.com"];
        cfg.AllowedDomains.Should().ContainSingle();
        cfg.BlockedDomains.Should().ContainSingle();

        var result = new WebSearchResult("title", "https://example.com", "snippet");
        result.Title.Should().Be("title");
        result.Url.Should().Be("https://example.com");
    }

    [Fact]
    public void AgentMetricsSnapshot_exposes_all_fields()
    {
        var snap = new AgentMetricsSnapshot(
            "agent-1",
            ExecutionCount: 10,
            SuccessCount: 8,
            FailureCount: 2,
            SuccessRate: 0.8,
            LastStartedAt: DateTimeOffset.UtcNow,
            LastCompletedAt: DateTimeOffset.UtcNow,
            LastError: "err",
            BackgroundAgentAggressivenessMode.Active);
        snap.AgentId.Should().Be("agent-1");
        snap.SuccessRate.Should().Be(0.8);
    }

    [Fact]
    public async Task NoOpPatternStore_returns_empty_query_results()
    {
        var store = new NoOpPatternStore();
        var results = await store.QueryAsync(new PatternStoreQueryParams(), CancellationToken.None);
        results.Should().BeEmpty();
        await store.PersistAsync();
    }

    [Fact]
    public void BingWebSearchProvider_rejects_null_dependencies()
    {
        var act = () => new BingWebSearchProvider(null!, "key");
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");

        var actKey = () => new BingWebSearchProvider(new HttpClient(), null!);
        actKey.Should().Throw<ArgumentNullException>().WithParameterName("subscriptionKey");
    }

    [Fact]
    public async Task BingWebSearchProvider_empty_query_returns_no_results()
    {
        var provider = new BingWebSearchProvider(new HttpClient(), "test-key");
        provider.ProviderId.Should().Be("bing");
        (await provider.SearchAsync("   ", 5)).Should().BeEmpty();
    }

    [Fact]
    public async Task BingWebSearchProvider_parses_successful_bing_response()
    {
        const string json = """
            {
              "webPages": {
                "value": [
                  { "name": "Ashlar docs", "url": "https://example.com/ashlar", "snippet": "kernel transport" },
                  { "name": "Other", "url": "https://example.com/other", "snippet": "skip me" }
                ]
              }
            }
            """;
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            request.Headers.Contains("Ocp-Apim-Subscription-Key").Should().BeTrue();
            request.RequestUri!.Query.Should().Contain("q=ashlar");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        });
        using var client = new HttpClient(handler);
        var provider = new BingWebSearchProvider(client, "test-key", "https://bing.test/search");

        var results = await provider.SearchAsync("ashlar", 1);

        results.Should().ContainSingle();
        results[0].Title.Should().Be("Ashlar docs");
        results[0].Url.Should().Be("https://example.com/ashlar");
        results[0].Snippet.Should().Be("kernel transport");
    }

    [Fact]
    public async Task BingWebSearchProvider_http_failure_returns_empty()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        using var client = new HttpClient(handler);
        var provider = new BingWebSearchProvider(client, "test-key", "https://bing.test/search");

        (await provider.SearchAsync("fail", 3)).Should().BeEmpty();
    }

    [Fact]
    public async Task BingWebSearchProvider_malformed_json_returns_empty()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json", Encoding.UTF8, "application/json"),
            }));
        using var client = new HttpClient(handler);
        var provider = new BingWebSearchProvider(client, "test-key", "https://bing.test/search");

        (await provider.SearchAsync("bad", 3)).Should().BeEmpty();
    }

    [Fact]
    public void ConfigurableSensitivityLevel_exposes_policy_fields()
    {
        var level = new ConfigurableSensitivityLevel(
            "custom",
            "Custom",
            50,
            AllowsExternalLLM: false,
            AllowsWebSearch: true,
            RequiresLocalOnly: true,
            AllowsNetworkExports: false,
            "Custom level");
        level.Value.Should().Be("custom");
        level.RequiresLocalOnly.Should().BeTrue();
        level.AllowsWebSearch.Should().BeTrue();
    }

}
