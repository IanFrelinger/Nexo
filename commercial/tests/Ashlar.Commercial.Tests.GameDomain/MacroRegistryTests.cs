using FluentAssertions;
using Ashlar.Commercial.GameDomain.Macros;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for macro registry.</summary>
public class MacroRegistryTests
{
    /// <summary>Creates macro.</summary>
    /// <param name="id">Id.</param>
    /// <param name="Macro"">Macro".</param>
    private static MacroDefinition CreateMacro(string id, string name = "Test Macro") =>
        new()
        {
            MacroId = id,
            DisplayName = name,
            Description = "A test macro",
            Author = "tester",
            Version = "1.0.0",
            Tags = ["test"],
            Parameters =
            [
                new MacroParameter { Name = "count", Type = "int", DefaultValue = 5, Min = 1, Max = 100 }
            ],
            Steps =
            [
                new MacroStep
                {
                    Action = "spawn_weapon",
                    Args = new Dictionary<string, object> { ["weaponId"] = "blaster" }
                }
            ],
        };

    [Fact]
    public void Register_And_Retrieve_ReturnsRegisteredMacro()
    {
        var registry = new MacroRegistry();
        var macro = CreateMacro("macro-1");

        registry.Register(macro);
        var retrieved = registry.Get("macro-1");

        retrieved.Should().NotBeNull();
        retrieved!.MacroId.Should().Be("macro-1");
        retrieved.DisplayName.Should().Be("Test Macro");
    }

    [Fact]
    public void List_ReturnsAllRegisteredMacros()
    {
        var registry = new MacroRegistry();
        registry.Register(CreateMacro("m1", "Macro 1"));
        registry.Register(CreateMacro("m2", "Macro 2"));
        registry.Register(CreateMacro("m3", "Macro 3"));

        var all = registry.List();

        all.Should().HaveCount(3);
        all.Select(m => m.MacroId).Should().BeEquivalentTo(["m1", "m2", "m3"]);
    }

    [Fact]
    public void Remove_DeletesMacro_AndReturnsTrue()
    {
        var registry = new MacroRegistry();
        registry.Register(CreateMacro("m1"));

        var removed = registry.Remove("m1");

        removed.Should().BeTrue();
        registry.Get("m1").Should().BeNull();
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenMacroNotFound()
    {
        var registry = new MacroRegistry();

        var removed = registry.Remove("nonexistent");

        removed.Should().BeFalse();
    }

    [Fact]
    public void ConcurrentRegistration_IsThreadSafe()
    {
        var registry = new MacroRegistry();
        const int count = 500;

        Parallel.For(0, count, i =>
        {
            registry.Register(CreateMacro($"macro-{i}", $"Macro {i}"));
        });

        var all = registry.List();
        all.Should().HaveCount(count);
    }

    [Fact]
    public void ImportExport_RoundTrip_ViaExporter()
    {
        var original = CreateMacro("roundtrip-1", "Round-Trip Macro");

        var json = MacroExporter.ExportToJson(original);
        var reimported = MacroExporter.ImportFromJson(json);

        reimported.MacroId.Should().Be(original.MacroId);
        reimported.DisplayName.Should().Be(original.DisplayName);
        reimported.Description.Should().Be(original.Description);
        reimported.Author.Should().Be(original.Author);
        reimported.Version.Should().Be(original.Version);
        reimported.Tags.Should().BeEquivalentTo(original.Tags);
        reimported.Parameters.Should().HaveCount(original.Parameters.Count);
        reimported.Steps.Should().HaveCount(original.Steps.Count);
    }

    [Fact]
    public void Export_ReturnsNull_ForMissingMacro()
    {
        var registry = new MacroRegistry();

        var result = registry.Export("does-not-exist");

        result.Should().BeNull();
    }

    [Fact]
    public void Import_RegistersMacro_ThenRetrievable()
    {
        var registry = new MacroRegistry();
        var macro = CreateMacro("imported-1");

        registry.Import(macro);
        var retrieved = registry.Get("imported-1");

        retrieved.Should().NotBeNull();
        retrieved!.MacroId.Should().Be("imported-1");
    }
}
