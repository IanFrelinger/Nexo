using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.BackgroundAgents.Trust;
using Nexo.BackgroundAgents.WebSearch;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

public class SanitizingProviderFactoryTests
{
    [Fact]
    public async Task ExecuteLLMAsync_WhenProxyAllows_DelegatesToInner()
    {
        var innerMock = new Mock<IProviderFactory>();
        innerMock.Setup(x => x.ExecuteLLMAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("response");

        var proxy = new CloudSanitizationProxy(contentFilter: null);
        var factory = new SanitizingProviderFactory(
            innerMock.Object,
            proxy,
            NullLogger<SanitizingProviderFactory>.Instance);

        var result = await factory.ExecuteLLMAsync("mock", "sys", "user", new { }, default);

        result.Should().Be("response");
        innerMock.Verify(x => x.ExecuteLLMAsync("mock", "sys", "user", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteLLMAsync_WhenProxyBlocks_Throws()
    {
        var innerMock = new Mock<IProviderFactory>();
        var filter = new Nexo.BackgroundAgents.WebSearch.SensitiveContentFilter();
        var proxy = new CloudSanitizationProxy(filter);
        var factory = new SanitizingProviderFactory(
            innerMock.Object,
            proxy,
            NullLogger<SanitizingProviderFactory>.Instance);

        var act = () => factory.ExecuteLLMAsync("mock", "sys", "user@secret.com", new { }, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*blocked*");
        innerMock.Verify(x => x.ExecuteLLMAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void IsProviderAvailable_DelegatesToInner()
    {
        var innerMock = new Mock<IProviderFactory>();
        innerMock.Setup(x => x.IsProviderAvailable("ollama")).Returns(true);

        var proxy = new CloudSanitizationProxy(contentFilter: null);
        var factory = new SanitizingProviderFactory(
            innerMock.Object,
            proxy,
            NullLogger<SanitizingProviderFactory>.Instance);

        factory.IsProviderAvailable("ollama").Should().BeTrue();
    }
}
