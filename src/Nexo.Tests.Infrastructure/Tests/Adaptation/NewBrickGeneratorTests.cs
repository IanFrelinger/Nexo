using FluentAssertions;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Infrastructure.Adaptation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>Tests for new brick generator.</summary>
[Trait("Category", "Adaptation")]
public sealed class NewBrickGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsStubManifest()
    {
        var generator = new NewBrickGenerator();
        var manifest = await generator.GenerateAsync("repeated-edits");

        manifest.Id.Should().Contain("generated");
        manifest.Name.Should().Contain("repeated-edits");
        manifest.Interface.Outputs.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_ErrorSummaryExtractor_EmitsDeterministicSource()
    {
        var generator = new NewBrickGenerator();
        var manifest = await generator.GenerateAsync("ErrorSummaryExtractor");

        manifest.Id.Should().Be("error-summary-extractor");
        manifest.ImplementationSource.Should().NotBeNullOrWhiteSpace();
        manifest.ImplementationSource.Should().Contain("errorCount");
        manifest.ImplementationSource.Should().Contain("firstErrorMessage");
        manifest.Interface.Inputs.Should().ContainSingle(i => i.Name == "logText");
    }
}
