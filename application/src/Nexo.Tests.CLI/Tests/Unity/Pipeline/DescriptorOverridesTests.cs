using FluentAssertions;
using Nexo.CLI.Unity.Pipeline;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Unity.Pipeline;

public sealed class DescriptorOverridesTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"nexo-overrides-{Guid.NewGuid():N}");
    private readonly string _descriptorPath;

    public DescriptorOverridesTests()
    {
        Directory.CreateDirectory(_tempDir);
        _descriptorPath = Path.Combine(_tempDir, "weapon.descriptor.json");
        File.WriteAllText(_descriptorPath, "{}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmptyDictionary()
    {
        var nonExistent = Path.Combine(_tempDir, "nope.json");
        var result = DescriptorOverrides.Load(nonExistent);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Pin_CreatesOverrideFile()
    {
        DescriptorOverrides.Pin(_descriptorPath, "damage", 42);

        var overridePath = DescriptorOverrides.GetOverridePath(_descriptorPath);
        File.Exists(overridePath).Should().BeTrue();
    }

    [Fact]
    public void Pin_And_Load_ReturnsPinnedValue()
    {
        DescriptorOverrides.Pin(_descriptorPath, "damage", 42);
        DescriptorOverrides.Pin(_descriptorPath, "name", "Plasma Rifle");

        var overrides = DescriptorOverrides.Load(_descriptorPath);
        overrides.Should().ContainKey("damage");
        overrides.Should().ContainKey("name");
    }

    [Fact]
    public void Unpin_RemovesField()
    {
        DescriptorOverrides.Pin(_descriptorPath, "damage", 42);
        DescriptorOverrides.Pin(_descriptorPath, "name", "Plasma Rifle");

        DescriptorOverrides.Unpin(_descriptorPath, "damage");

        var overrides = DescriptorOverrides.Load(_descriptorPath);
        overrides.Should().NotContainKey("damage");
        overrides.Should().ContainKey("name");
    }

    [Fact]
    public void Unpin_LastField_DeletesFile()
    {
        DescriptorOverrides.Pin(_descriptorPath, "damage", 42);
        DescriptorOverrides.Unpin(_descriptorPath, "damage");

        var overridePath = DescriptorOverrides.GetOverridePath(_descriptorPath);
        File.Exists(overridePath).Should().BeFalse();
    }

    [Fact]
    public void ToPromptFragment_WithPins_ContainsPinnedMarker()
    {
        DescriptorOverrides.Pin(_descriptorPath, "damage", 42);
        DescriptorOverrides.Pin(_descriptorPath, "name", "Railgun");

        var fragment = DescriptorOverrides.ToPromptFragment(_descriptorPath);

        fragment.Should().Contain("PINNED");
        fragment.Should().Contain("damage");
        fragment.Should().Contain("name");
    }

    [Fact]
    public void ToPromptFragment_NoPins_ReturnsEmpty()
    {
        var fragment = DescriptorOverrides.ToPromptFragment(_descriptorPath);
        fragment.Should().BeEmpty();
    }

    [Fact]
    public void GetOverridePath_ChangesExtension()
    {
        var result = DescriptorOverrides.GetOverridePath("/some/path/weapon.descriptor.json");
        result.Should().EndWith(".overrides.json");
    }
}
