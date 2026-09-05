using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.BackgroundAgents.WebSearch;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for web search CLI validation.</summary>
public class WebSearchCommandTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task TestAsync_refusesNonPositiveMaxResultsBeforeProviderCall(int maxResults)
    {
        var provider = new Mock<IWebSearchProvider>(MockBehavior.Strict);
        var command = new WebSearchCommand(
            new ConfigurationBuilder().Build(),
            NullLogger<WebSearchCommand>.Instance,
            provider.Object);

        var exitCode = await command.TestAsync("ashlar", maxResults, formatJson: true);

        exitCode.Should().Be(1);
        provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestAsync_acceptsPositiveMaxResults()
    {
        var provider = new Mock<IWebSearchProvider>(MockBehavior.Strict);
        provider.SetupGet(x => x.ProviderId).Returns("test");
        provider
            .Setup(x => x.SearchAsync("ashlar", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WebSearchResult>());
        var command = new WebSearchCommand(
            new ConfigurationBuilder().Build(),
            NullLogger<WebSearchCommand>.Instance,
            provider.Object);

        var exitCode = await command.TestAsync("ashlar", 1, formatJson: true);

        exitCode.Should().Be(0);
        provider.Verify(
            x => x.SearchAsync("ashlar", 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
