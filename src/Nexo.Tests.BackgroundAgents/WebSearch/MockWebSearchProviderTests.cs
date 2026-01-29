using FluentAssertions;
using Nexo.BackgroundAgents.WebSearch;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.WebSearch;

public class MockWebSearchProviderTests
{
    [Fact]
    public void ProviderId_IsMock()
    {
        var provider = new MockWebSearchProvider();
        provider.ProviderId.Should().Be("mock");
    }

    [Fact]
    public async Task SearchAsync_WhenNoResults_ReturnsEmpty()
    {
        var provider = new MockWebSearchProvider();
        var results = await provider.SearchAsync("anything", 5, default);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WhenDefaultResultsSet_ReturnsThem()
    {
        var defaultResults = new[]
        {
            new WebSearchResult("Title A", "https://a.com", "Snippet A"),
            new WebSearchResult("Title B", "https://b.com", "Snippet B")
        };
        var provider = new MockWebSearchProvider(defaultResults: defaultResults);
        var results = await provider.SearchAsync("query", 10, default);
        results.Should().HaveCount(2);
        results[0].Title.Should().Be("Title A");
        results[1].Url.Should().Be("https://b.com");
    }

    [Fact]
    public async Task SearchAsync_WhenQueryResultsSet_ReturnsMatching()
    {
        var queryResults = new Dictionary<string, IReadOnlyList<WebSearchResult>>(StringComparer.OrdinalIgnoreCase)
        {
            ["nexo"] = new[] { new WebSearchResult("Nexo", "https://nexo.org", "Game framework") }
        };
        var provider = new MockWebSearchProvider(queryResults: queryResults);
        var results = await provider.SearchAsync("nexo", 5, default);
        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Nexo");
    }

    [Fact]
    public async Task SearchAsync_RespectsMaxResults()
    {
        var defaultResults = new[]
        {
            new WebSearchResult("A", "https://a.com", "a"),
            new WebSearchResult("B", "https://b.com", "b"),
            new WebSearchResult("C", "https://c.com", "c")
        };
        var provider = new MockWebSearchProvider(defaultResults: defaultResults);
        var results = await provider.SearchAsync("q", 2, default);
        results.Should().HaveCount(2);
    }
}
