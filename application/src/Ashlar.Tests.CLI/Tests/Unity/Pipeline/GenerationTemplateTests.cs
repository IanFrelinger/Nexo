using FluentAssertions;
using Ashlar.CLI.Unity.Pipeline;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Unity.Pipeline;

/// <summary>Tests for generation template.</summary>
public sealed class GenerationTemplateTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"ashlar-template-{Guid.NewGuid():N}");

    public GenerationTemplateTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void LoadFromFile_NullPath_ReturnsNull()
    {
        GenerationTemplate.LoadFromFile(null!).Should().BeNull();
    }

    [Fact]
    public void LoadFromFile_EmptyPath_ReturnsNull()
    {
        GenerationTemplate.LoadFromFile("").Should().BeNull();
    }

    [Fact]
    public void LoadFromFile_MissingFile_ReturnsNull()
    {
        GenerationTemplate.LoadFromFile(Path.Combine(_tempDir, "nope.json")).Should().BeNull();
    }

    [Fact]
    public void LoadFromFile_WithGenerateField_ParsesCorrectly()
    {
        var templatePath = Path.Combine(_tempDir, "weapon.template.json");
        File.WriteAllText(templatePath, @"{
            ""name"": ""Assault Rifle"",
            ""category"": ""primary"",
            ""damage"": 25.5,
            ""automatic"": true,
            ""_generate"": [""fireRate"", ""reloadTime"", ""magazineSize""]
        }");

        var template = GenerationTemplate.LoadFromFile(templatePath);

        template.Should().NotBeNull();
        template!.FixedValues.Should().ContainKey("name");
        template.FixedValues["name"].Should().Be("Assault Rifle");
        template.FixedValues["category"].Should().Be("primary");
        template.FixedValues["damage"].Should().Be(25.5);
        template.FixedValues["automatic"].Should().Be(true);
        template.GenerateFields.Should().BeEquivalentTo("fireRate", "reloadTime", "magazineSize");
    }

    [Fact]
    public void LoadFromFile_NoGenerateField_HasEmptyGenerateFields()
    {
        var templatePath = Path.Combine(_tempDir, "simple.template.json");
        File.WriteAllText(templatePath, @"{ ""name"": ""Shield"", ""health"": 100 }");

        var template = GenerationTemplate.LoadFromFile(templatePath);

        template.Should().NotBeNull();
        template!.GenerateFields.Should().BeEmpty();
        template.FixedValues.Should().HaveCount(2);
    }

    [Fact]
    public void ToPromptFragment_WithFixedAndGenerate_FormatsCorrectly()
    {
        var template = new GenerationTemplate
        {
            FixedValues = new Dictionary<string, object>
            {
                ["name"] = "Shotgun",
                ["damage"] = 80.0
            },
            GenerateFields = new[] { "fireRate", "spread" }
        };

        var fragment = template.ToPromptFragment();

        fragment.Should().Contain("Fixed values from template");
        fragment.Should().Contain("name: Shotgun");
        fragment.Should().Contain("damage: 80");
        fragment.Should().Contain("Generate values for: fireRate, spread");
    }

    [Fact]
    public void ToPromptFragment_Empty_ReturnsEmpty()
    {
        var template = new GenerationTemplate();
        template.ToPromptFragment().Should().BeEmpty();
    }

    [Fact]
    public void LoadFromFile_BoolFalsePreserved()
    {
        var templatePath = Path.Combine(_tempDir, "bool.template.json");
        File.WriteAllText(templatePath, @"{ ""enabled"": false }");

        var template = GenerationTemplate.LoadFromFile(templatePath);

        template.Should().NotBeNull();
        template!.FixedValues["enabled"].Should().Be(false);
    }
}
