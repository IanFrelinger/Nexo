using FluentAssertions;
using Nexo.BackgroundAgents.WebSearch;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.WebSearch;

/// <summary>Tests for sensitive content filter.</summary>
public class SensitiveContentFilterTests
{
    [Fact]
    public void ShouldBlockQuery_WhenQueryContainsEmail_ReturnsTrue()
    {
        var filter = new SensitiveContentFilter(blockQueriesWithPii: true);
        filter.ShouldBlockQuery("contact me at user@example.com").Should().BeTrue();
    }

    [Fact]
    public void ShouldBlockQuery_WhenQueryContainsPhone_ReturnsTrue()
    {
        var filter = new SensitiveContentFilter(blockQueriesWithPii: true);
        filter.ShouldBlockQuery("call 555-123-4567").Should().BeTrue();
    }

    [Fact]
    public void ShouldBlockQuery_WhenQueryContainsSsnLike_ReturnsTrue()
    {
        var filter = new SensitiveContentFilter(blockQueriesWithPii: true);
        filter.ShouldBlockQuery("SSN 123-45-6789").Should().BeTrue();
    }

    [Fact]
    public void ShouldBlockQuery_WhenNoPii_ReturnsFalse()
    {
        var filter = new SensitiveContentFilter(blockQueriesWithPii: true);
        filter.ShouldBlockQuery("what is the weather today").Should().BeFalse();
    }

    [Fact]
    public void FilterQuery_RedactsEmail()
    {
        var filter = new SensitiveContentFilter();
        filter.FilterQuery("email user@example.com here").Should().Be("email [REDACTED] here");
    }

    [Fact]
    public void FilterSnippet_RedactsPhone()
    {
        var filter = new SensitiveContentFilter();
        filter.FilterSnippet("Call 555-123-4567 for info").Should().Contain("[REDACTED]");
    }
}
