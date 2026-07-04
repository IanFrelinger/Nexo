using FluentAssertions;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Infrastructure.Adaptation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>Tests for new behavior assembler.</summary>
[Trait("Category", "Adaptation")]
public sealed class NewBehaviorAssemblerTests
{
    [Fact]
    public async Task AssembleAsync_ReturnsBehaviorWithSteps()
    {
        var assembler = new NewBehaviorAssembler();
        var behavior = await assembler.AssembleAsync("test goal", ["brick-a", "brick-b"]);

        behavior.Should().NotBeNull();
        behavior.Steps.Should().HaveCount(2);
        behavior.Steps[0].BrickId.Should().Be("brick-a");
        behavior.Steps[1].BrickId.Should().Be("brick-b");
        behavior.Description.Should().Be("test goal");
    }
}
