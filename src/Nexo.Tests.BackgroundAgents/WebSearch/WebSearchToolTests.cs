using System.Text.Json;
using FluentAssertions;
using Moq;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.WebSearch;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.WebSearch;

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
        var results = new[] { new WebSearchResult("Nexo", "https://nexo.org", "A framework") };
        var provider = new Mock<IWebSearchProvider>();
        provider.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);
        var tool = new WebSearchTool(provider.Object);
        var args = JsonSerializer.SerializeToElement(new { query = "nexo framework" });
        var call = new ToolCall("web_search", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().Contain(l => l.Contains("Web search"));
        result.Payload.Should().NotBeNull();
        var list = result.Payload as System.Collections.IList;
        list.Should().NotBeNull();
        list!.Count.Should().Be(1);
        provider.Verify(p => p.SearchAsync("nexo framework", 10, It.IsAny<CancellationToken>()), Times.Once);
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
}
