using System.Text.Json;
using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.WebSearch;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.WebSearch;

/// <summary>Tests for web search tool.</summary>
public class WebSearchToolTests
{
    [Fact]
    public void Id_IsWebSearch()
    {
        var tool = new WebSearchTool(Mock.Of<IWebSearchProvider>());
        tool.Id.Should().Be("web_search");
    }

    [Fact]
    public async Task InvokeAsync_CallsProvider_AndReturnsPayload()
    {
        var results = new[] { new WebSearchResult("Ashlar", "https://ashlar.org", "A framework") };
        var provider = new Mock<IWebSearchProvider>();
        provider.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);
        var tool = new WebSearchTool(provider.Object);
        var args = JsonSerializer.SerializeToElement(new { query = "ashlar framework" });
        var call = new ToolCall("web_search", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().Contain(l => l.Contains("Web search"));
        result.Payload.Should().NotBeNull();
        var list = result.Payload as System.Collections.IList;
        list.Should().NotBeNull();
        list!.Count.Should().Be(1);
        provider.Verify(p => p.SearchAsync("ashlar framework", 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenContentFilterBlocksQuery_ReturnsEmptyWithoutCallingProvider()
    {
        var provider = new Mock<IWebSearchProvider>();
        var filter = new SensitiveContentFilter(blockQueriesWithPii: true);
        var tool = new WebSearchTool(provider.Object, contentFilter: filter);
        var args = JsonSerializer.SerializeToElement(new { query = "email me at user@example.com" });
        var call = new ToolCall("web_search", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().NotBeEmpty();
        result.Delta.Log[0].Should().Contain("sensitive content");
        result.Payload.Should().NotBeNull();
        var list = result.Payload as System.Collections.IList;
        list.Should().NotBeNull();
        list!.Count.Should().Be(0);
        provider.Verify(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenAllowedDomainsSet_FiltersResults()
    {
        var results = new[]
        {
            new WebSearchResult("A", "https://wikipedia.org/page", "a"),
            new WebSearchResult("B", "https://other.com/page", "b")
        };
        var provider = new MockWebSearchProvider(defaultResults: results);
        var tool = new WebSearchTool(provider, allowedDomains: new[] { "wikipedia.org" });
        var args = JsonSerializer.SerializeToElement(new { query = "test" });
        var call = new ToolCall("web_search", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        var list = (result.Payload as System.Collections.IList)!;
        list.Should().NotBeNull();
        list.Count.Should().Be(1);
        ((WebSearchResult)list[0]!).Url.Should().Contain("wikipedia.org");
    }

    [Fact]
    public async Task InvokeAsync_WhenBlockedDomainsSet_ExcludesResults()
    {
        var results = new[]
        {
            new WebSearchResult("A", "https://spam.com/page", "a"),
            new WebSearchResult("B", "https://good.com/page", "b")
        };
        var provider = new MockWebSearchProvider(defaultResults: results);
        var tool = new WebSearchTool(provider, blockedDomains: new[] { "spam.com" });
        var args = JsonSerializer.SerializeToElement(new { query = "test" });
        var call = new ToolCall("web_search", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        var list = (result.Payload as System.Collections.IList)!;
        list.Count.Should().Be(1);
        ((WebSearchResult)list[0]!).Url.Should().Contain("good.com");
    }

    [Fact]
    public async Task InvokeAsync_uses_custom_maxResults_and_default_when_unspecified()
    {
        var provider = new Mock<IWebSearchProvider>();
        provider.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WebSearchResult>());
        var tool = new WebSearchTool(provider.Object, defaultMaxResults: 3);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        await tool.InvokeAsync(
            new ToolCall("web_search", JsonSerializer.SerializeToElement(new { query = "a", maxResults = 7 })),
            snapshot,
            default);
        provider.Verify(p => p.SearchAsync("a", 7, It.IsAny<CancellationToken>()), Times.Once);

        await tool.InvokeAsync(
            new ToolCall("web_search", JsonSerializer.SerializeToElement(new { query = "b" })),
            snapshot,
            default);
        provider.Verify(p => p.SearchAsync("b", 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_InvalidJsonArgs_uses_defaults()
    {
        var provider = new Mock<IWebSearchProvider>();
        provider.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WebSearchResult>());
        var tool = new WebSearchTool(provider.Object);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        await tool.InvokeAsync(new ToolCall("web_search", JsonSerializer.SerializeToElement("{bad")), snapshot, default);

        provider.Verify(p => p.SearchAsync("", 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_FiltersSnippets_when_content_filter_enabled()
    {
        var results = new[] { new WebSearchResult("A", "https://good.com/page", "contact user@example.com") };
        var provider = new MockWebSearchProvider(defaultResults: results);
        var filter = new SensitiveContentFilter(blockQueriesWithPii: false);
        var tool = new WebSearchTool(provider, contentFilter: filter);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(
            new ToolCall("web_search", JsonSerializer.SerializeToElement(new { query = "safe query" })),
            snapshot,
            default);

        var list = (result.Payload as System.Collections.IList)!;
        ((WebSearchResult)list[0]!).Snippet.Should().NotContain("user@example.com");
    }

    [Fact]
    public async Task InvokeAsync_Skips_results_with_invalid_urls()
    {
        var results = new[]
        {
            new WebSearchResult("Bad", "not-a-url", "a"),
            new WebSearchResult("Good", "https://www.allowed.org/page", "b"),
        };
        var provider = new MockWebSearchProvider(defaultResults: results);
        var tool = new WebSearchTool(provider, allowedDomains: new[] { "www.allowed.org" });
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(
            new ToolCall("web_search", JsonSerializer.SerializeToElement(new { query = "test" })),
            snapshot,
            default);

        var list = (result.Payload as System.Collections.IList)!;
        list.Count.Should().Be(1);
        ((WebSearchResult)list[0]!).Url.Should().Contain("allowed.org");
    }

    [Fact]
    public void Constructor_rejects_null_provider()
    {
        var act = () => new WebSearchTool(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
