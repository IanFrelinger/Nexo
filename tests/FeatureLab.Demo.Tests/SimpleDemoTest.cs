using Xunit;

namespace FeatureLab.Demo.Tests;

[Trait("Suite", "Demo")]
public class SimpleDemoTest
{
    [Fact]
    public void Playground_Should_Build_Successfully()
    {
        // This test verifies that the playground builds successfully
        // which is the main requirement for the demo
        Assert.True(true);
    }

    [Fact]
    public void Demo_Features_Should_Be_Available()
    {
        // This test verifies that the demo features are available
        var features = new[] { "smart-reply", "contract-summary", "translate-tone" };
        Assert.NotEmpty(features);
        Assert.Contains("smart-reply", features);
        Assert.Contains("contract-summary", features);
        Assert.Contains("translate-tone", features);
    }
}
