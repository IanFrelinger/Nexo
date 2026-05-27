using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.BackgroundAgents.Trust;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

public sealed class SanitizingProviderFactoryGapCoverageTests
{
    [Fact]
    public async Task ExecuteLLMAsync_blocks_when_sanitizer_denies()
    {
        var inner = new Mock<IProviderFactory>();
        var proxy = new Mock<ICloudSanitizationProxy>();
        proxy.Setup(p => p.SanitizeForCloud(It.IsAny<OutgoingContext>(), It.IsAny<CancellationToken>()))
            .Returns(SanitizationResult.Blocked("blocked"));

        var factory = new SanitizingProviderFactory(inner.Object, proxy.Object, NullLogger<SanitizingProviderFactory>.Instance);

        var act = () => factory.ExecuteLLMAsync("openai", "sys", "user", new { }, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*blocked*");
        inner.Verify(i => i.ExecuteLLMAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteLLMAsync_delegates_sanitized_prompts()
    {
        var inner = new Mock<IProviderFactory>();
        inner.Setup(i => i.ExecuteLLMAsync("openai", "clean-sys", "clean-user", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        var proxy = new Mock<ICloudSanitizationProxy>();
        proxy.Setup(p => p.SanitizeForCloud(It.IsAny<OutgoingContext>(), It.IsAny<CancellationToken>()))
            .Returns(SanitizationResult.AllowedWith(new OutgoingContext
            {
                SystemPrompt = "clean-sys",
                UserPrompt = "clean-user",
                Provider = "openai",
            }));

        var factory = new SanitizingProviderFactory(inner.Object, proxy.Object, NullLogger<SanitizingProviderFactory>.Instance);
        var result = await factory.ExecuteLLMAsync("openai", "dirty-sys", "dirty-user", new { }, CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task ExecuteVisionAsync_blocks_when_sanitizer_denies()
    {
        var inner = new Mock<IProviderFactory>();
        var proxy = new Mock<ICloudSanitizationProxy>();
        proxy.Setup(p => p.SanitizeForCloud(It.IsAny<OutgoingContext>(), It.IsAny<CancellationToken>()))
            .Returns(SanitizationResult.Blocked("vision blocked"));

        var factory = new SanitizingProviderFactory(inner.Object, proxy.Object, NullLogger<SanitizingProviderFactory>.Instance);
        var act = () => factory.ExecuteVisionAsync("openai", "sys", "user", new byte[] { 1 }, new { }, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*vision blocked*");
    }

    [Fact]
    public async Task ExecuteVisionMultiFrameAsync_delegates_when_allowed()
    {
        var inner = new Mock<IProviderFactory>();
        inner.Setup(i => i.ExecuteVisionMultiFrameAsync(
                "openai",
                "sys",
                "user",
                It.IsAny<IReadOnlyList<byte[]>>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("frames");

        var proxy = new Mock<ICloudSanitizationProxy>();
        proxy.Setup(p => p.SanitizeForCloud(It.IsAny<OutgoingContext>(), It.IsAny<CancellationToken>()))
            .Returns(SanitizationResult.AllowedWith(new OutgoingContext
            {
                SystemPrompt = "sys",
                UserPrompt = "user",
                Provider = "openai",
            }));

        var factory = new SanitizingProviderFactory(inner.Object, proxy.Object, NullLogger<SanitizingProviderFactory>.Instance);
        var result = await factory.ExecuteVisionMultiFrameAsync(
            "openai",
            "sys",
            "user",
            new[] { new byte[] { 1 }, new byte[] { 2 } },
            new { },
            CancellationToken.None);

        result.Should().Be("frames");
    }

    [Fact]
    public async Task ExecuteVideoAsync_delegates_when_allowed()
    {
        var inner = new Mock<IProviderFactory>();
        inner.Setup(i => i.ExecuteVideoAsync("clean-sys", "clean-user", It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("video-ok");

        var proxy = new Mock<ICloudSanitizationProxy>();
        proxy.Setup(p => p.SanitizeForCloud(It.IsAny<OutgoingContext>(), It.IsAny<CancellationToken>()))
            .Returns(SanitizationResult.AllowedWith(new OutgoingContext
            {
                SystemPrompt = "clean-sys",
                UserPrompt = "clean-user",
                Provider = "video",
            }));

        var factory = new SanitizingProviderFactory(inner.Object, proxy.Object, NullLogger<SanitizingProviderFactory>.Instance);
        var result = await factory.ExecuteVideoAsync("dirty-sys", "dirty-user", new[] { new byte[] { 1 } }, new { }, CancellationToken.None);

        result.Should().Be("video-ok");
    }

    [Fact]
    public async Task ExecuteVisionAsync_delegates_when_allowed()
    {
        var inner = new Mock<IProviderFactory>();
        inner.Setup(i => i.ExecuteVisionAsync("openai", "sys", "user", It.IsAny<byte[]>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("vision-ok");

        var proxy = new Mock<ICloudSanitizationProxy>();
        proxy.Setup(p => p.SanitizeForCloud(It.IsAny<OutgoingContext>(), It.IsAny<CancellationToken>()))
            .Returns(SanitizationResult.AllowedWith(new OutgoingContext
            {
                SystemPrompt = "sys",
                UserPrompt = "user",
                Provider = "openai",
            }));

        var factory = new SanitizingProviderFactory(inner.Object, proxy.Object, NullLogger<SanitizingProviderFactory>.Instance);
        var result = await factory.ExecuteVisionAsync("openai", "sys", "user", new byte[] { 1 }, new { }, CancellationToken.None);

        result.Should().Be("vision-ok");
    }

    [Fact]
    public async Task ExecuteVideoAsync_blocks_when_sanitizer_denies()
    {
        var inner = new Mock<IProviderFactory>();
        var proxy = new Mock<ICloudSanitizationProxy>();
        proxy.Setup(p => p.SanitizeForCloud(It.IsAny<OutgoingContext>(), It.IsAny<CancellationToken>()))
            .Returns(SanitizationResult.Blocked("video blocked"));

        var factory = new SanitizingProviderFactory(inner.Object, proxy.Object, NullLogger<SanitizingProviderFactory>.Instance);
        var act = () => factory.ExecuteVideoAsync("sys", "user", new[] { new byte[] { 1 } }, new { }, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*video blocked*");
    }

    [Fact]
    public void IsProviderAvailable_delegates_to_inner()
    {
        var inner = new Mock<IProviderFactory>();
        inner.Setup(i => i.IsProviderAvailable("ollama")).Returns(true);

        var proxy = new Mock<ICloudSanitizationProxy>();
        var factory = new SanitizingProviderFactory(inner.Object, proxy.Object, NullLogger<SanitizingProviderFactory>.Instance);

        factory.IsProviderAvailable("ollama").Should().BeTrue();
    }
}
