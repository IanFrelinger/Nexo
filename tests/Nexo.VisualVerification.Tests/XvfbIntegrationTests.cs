using FluentAssertions;
using Nexo.VisualVerification.Providers;
using Nexo.VisualVerification.Tests.Support;
using Xunit;

namespace Nexo.VisualVerification.Tests;

[Trait("Category", "XvfbIntegration")]
public sealed class XvfbIntegrationTests
{
    [Fact]
    public async Task XvfbDisplayProvider_WhenAvailable_CapturesNonEmptyFrame()
    {
        if (!XvfbDisplayProvider.IsAvailable())
        {
            return;
        }

        await using var display = new XvfbDisplayProvider();
        var config = await display.StartAsync(64, 64, CancellationToken.None);
        var pixels = await display.CaptureFrameAsync(CancellationToken.None);

        config.ProviderId.Should().Be("xvfb");
        pixels.Length.Should().Be(64 * 64 * 4);
        pixels.Any(b => b != 0).Should().BeTrue();
    }
}
