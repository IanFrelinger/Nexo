using FluentAssertions;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Infrastructure.Adaptation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

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
}
