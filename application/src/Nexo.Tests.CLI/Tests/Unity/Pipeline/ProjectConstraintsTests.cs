using System.Text.Json;
using FluentAssertions;
using Nexo.CLI.Unity.Pipeline;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Unity.Pipeline;

/// <summary>Tests for project constraints.</summary>
public sealed class ProjectConstraintsTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"nexo-constraints-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void LoadFromFile_MissingFile_ReturnsDefaults()
    {
        var result = ProjectConstraints.LoadFromFile(_tempDir);

        result.Should().NotBeNull();
        result.Code.NamespacePrefix.Should().BeNull();
        result.Code.MaxFileLines.Should().Be(0);
        result.Weapons.DamageRange.Should().BeNull();
    }

    [Fact]
    public void SaveToFile_CreatesFile()
    {
        var constraints = new ProjectConstraints
        {
            Code = new CodeConstraints { NamespacePrefix = "MyGame", MaxFileLines = 200 }
        };

        constraints.SaveToFile(_tempDir);

        var path = Path.Combine(_tempDir, ".nexo", "constraints.json");
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var original = new ProjectConstraints
        {
            Code = new CodeConstraints
            {
                NamespacePrefix = "Acme.FPS",
                MaxFileLines = 300,
                RequireInterfaces = true,
                BannedPatterns = new[] { "Singleton", "FindObjectOfType" },
                TestCoverage = "80%"
            },
            Weapons = new WeaponConstraints
            {
                DamageRange = new[] { 5.0, 100.0 },
                MaxMagazineSize = 30
            },
            Movement = new MovementConstraints { MaxSpeed = 12.5 },
            Networking = new NetworkingConstraints { Authority = "server" },
            Audio = new AudioConstraints { RequireVariations = true, MinVariationsPerEvent = 3 },
            Aesthetics = new AestheticConstraints { MaxTriBudgetPerObject = 5000 }
        };

        original.SaveToFile(_tempDir);
        var loaded = ProjectConstraints.LoadFromFile(_tempDir);

        loaded.Code.NamespacePrefix.Should().Be("Acme.FPS");
        loaded.Code.MaxFileLines.Should().Be(300);
        loaded.Code.RequireInterfaces.Should().BeTrue();
        loaded.Code.BannedPatterns.Should().BeEquivalentTo("Singleton", "FindObjectOfType");
        loaded.Code.TestCoverage.Should().Be("80%");
        loaded.Weapons.DamageRange.Should().BeEquivalentTo(new[] { 5.0, 100.0 });
        loaded.Weapons.MaxMagazineSize.Should().Be(30);
        loaded.Movement.MaxSpeed.Should().Be(12.5);
        loaded.Networking.Authority.Should().Be("server");
        loaded.Audio.RequireVariations.Should().BeTrue();
        loaded.Audio.MinVariationsPerEvent.Should().Be(3);
        loaded.Aesthetics.MaxTriBudgetPerObject.Should().Be(5000);
    }

    [Fact]
    public void ToPromptFragment_WithConstraints_ContainsExpectedLines()
    {
        var constraints = new ProjectConstraints
        {
            Code = new CodeConstraints
            {
                NamespacePrefix = "MyGame",
                MaxFileLines = 200,
                RequireInterfaces = true,
                BannedPatterns = new[] { "Singleton" },
                TestCoverage = "90%"
            },
            Weapons = new WeaponConstraints
            {
                DamageRange = new[] { 10.0, 50.0 },
                MaxMagazineSize = 15
            },
            Movement = new MovementConstraints { MaxSpeed = 10 },
            Networking = new NetworkingConstraints { Authority = "server" },
            Audio = new AudioConstraints { RequireVariations = true, MinVariationsPerEvent = 4 },
            Aesthetics = new AestheticConstraints { MaxTriBudgetPerObject = 3000 }
        };

        var fragment = constraints.ToPromptFragment();

        fragment.Should().Contain("Use namespace prefix: MyGame");
        fragment.Should().Contain("Max 200 lines per file");
        fragment.Should().Contain("Create interfaces for all public contracts");
        fragment.Should().Contain("Never use: Singleton");
        fragment.Should().Contain("Test coverage: 90%");
        fragment.Should().Contain("Weapon damage must be between 10 and 50");
        fragment.Should().Contain("Max magazine size: 15");
        fragment.Should().Contain("Max movement speed: 10");
        fragment.Should().Contain("Network authority model: server");
        fragment.Should().Contain("Audio: require at least 4 variations per event");
        fragment.Should().Contain("Max 3000 triangles per object");
    }

    [Fact]
    public void ToPromptFragment_EmptyConstraints_ReturnsEmpty()
    {
        var constraints = new ProjectConstraints();
        constraints.ToPromptFragment().Should().BeEmpty();
    }
}
