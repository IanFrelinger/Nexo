using Ashlar.Infrastructure.Composition;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Composition;

/// <summary>Tests for composition registry validation.</summary>
public sealed class CompositionRegistryValidationTests
{
    [Fact]
    public void ValidateSelection_DefaultPipelineDescriptors_ReturnsNoIssues()
    {
        var registry = new CapabilityComponentRegistry();
        var issues = registry.ValidateSelection(
            new[] { "perception", "validation", "reporting" },
            new[] { "perception", "validation", "reporting" });

        Assert.Empty(issues);
    }

    [Fact]
    public void ValidateSelection_MissingRequiredCapability_ReturnsActionableIssue()
    {
        var registry = new CapabilityComponentRegistry();
        var issues = registry.ValidateSelection(
            new[] { "validation" },
            new[] { "validation" });

        Assert.Contains(issues, issue => issue.Code == "selection.missing-required-capability");
    }

    [Fact]
    public void ValidateSelection_IncompatibleComponents_ReturnsActionableIssue()
    {
        var registry = new CapabilityComponentRegistry();
        var issues = registry.ValidateSelection(
            new[] { "ui-interaction", "process-control" },
            new[] { "ui-interaction", "process-control" });

        Assert.Contains(issues, issue => issue.Code == "selection.incompatible-components");
    }

    [Fact]
    public async Task ComposeAsync_MissingDependencyAfterFiltering_ThrowsInvalidOperationException()
    {
        var registry = new CapabilityComponentRegistry();
        var engine = new CompositionEngine(registry);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.ComposeAsync(
                "miscellaneous request with no strong keywords",
                new[] { "validation", "reporting" }));
    }
}
