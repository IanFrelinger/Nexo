using FluentAssertions;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Infrastructure.Adaptation;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Adaptation;

/// <summary>Tests for fix generator.</summary>
[Trait("Category", "Adaptation")]
public sealed class FixGeneratorTests
{
    /// <summary>Creates base manifest.</summary>
    private static BrickManifest CreateBaseManifest() => new()
    {
        Id = "test.brick",
        Name = "Test DomainBrick",
        Version = "1.0.0",
        Interface = new BrickInterface { Inputs = [], Outputs = [] },
    };

    [Fact]
    public async Task GenerateFixesAsync_EmptyCatch_ReturnsManifestWithConfig()
    {
        var generator = new FixGenerator();
        var baseManifest = CreateBaseManifest();
        var context = new FailureContext { FailureType = "EmptyCatch", TargetId = "test", Message = "Empty catch" };

        var fixes = await generator.GenerateFixesAsync(context, baseManifest);

        fixes.Should().ContainSingle();
        fixes[0].Config.Should().ContainKey("fix:emptyCatch");
        fixes[0].Config["fix:emptyCatch"].Should().Be("Add logging in catch blocks");
    }

    [Fact]
    public async Task GenerateFixesAsync_MissingOutput_ReturnsManifestWithConfig()
    {
        var generator = new FixGenerator();
        var baseManifest = CreateBaseManifest();
        var context = new FailureContext { FailureType = "MissingOutput", TargetId = "test", Message = "Missing output" };

        var fixes = await generator.GenerateFixesAsync(context, baseManifest);

        fixes.Should().ContainSingle();
        fixes[0].Config.Should().ContainKey("fix:missingOutput");
    }

    [Fact]
    public async Task GenerateFixesAsync_Generic_ReturnsVersionBumpedManifest()
    {
        var generator = new FixGenerator();
        var baseManifest = CreateBaseManifest();
        var context = new FailureContext { FailureType = "UnknownType", TargetId = "test", Message = "Something broke" };

        var fixes = await generator.GenerateFixesAsync(context, baseManifest);

        fixes.Should().ContainSingle();
        fixes[0].Version.Should().Be("1.0.1");
        fixes[0].Config.Should().ContainKey("fix:generic");
    }

    [Fact]
    public async Task GenerateFixesAsync_NullBaseManifest_ReturnsEmpty()
    {
        var generator = new FixGenerator();
        var context = new FailureContext { FailureType = "EmptyCatch", TargetId = "test", Message = "Empty catch" };

        var fixes = await generator.GenerateFixesAsync(context, baseManifest: null);

        fixes.Should().BeEmpty();
    }
}
