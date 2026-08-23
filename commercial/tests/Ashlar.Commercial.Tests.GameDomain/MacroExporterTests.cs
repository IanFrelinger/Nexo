using System.Text.Json;
using FluentAssertions;
using Ashlar.Commercial.GameDomain.Macros;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for macro exporter.</summary>
public class MacroExporterTests
{
    /// <summary>Creates macro.</summary>
    /// <param name=""macro-1"">"macro-1".</param>
    /// <param name="Macro"">Macro".</param>
    private static MacroDefinition CreateMacro(string id = "macro-1", string name = "Test Macro") =>
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
    public void ExportImport_SingleMacro_RoundTrips()
    {
        var original = CreateMacro("rt-single", "Round-Trip Single");

        var json = MacroExporter.ExportToJson(original);
        var restored = MacroExporter.ImportFromJson(json);

        restored.MacroId.Should().Be(original.MacroId);
        restored.DisplayName.Should().Be(original.DisplayName);
        restored.Description.Should().Be(original.Description);
        restored.Author.Should().Be(original.Author);
        restored.Version.Should().Be(original.Version);
        restored.Tags.Should().BeEquivalentTo(original.Tags);
        restored.Parameters.Should().HaveCount(original.Parameters.Count);
        restored.Parameters[0].Name.Should().Be("count");
        restored.Steps.Should().HaveCount(original.Steps.Count);
        restored.Steps[0].Action.Should().Be("spawn_weapon");
    }

    [Fact]
    public void ExportImportMany_RoundTrips()
    {
        var macros = new[]
        {
            CreateMacro("m1", "Macro One"),
            CreateMacro("m2", "Macro Two"),
            CreateMacro("m3", "Macro Three"),
        };

        var json = MacroExporter.ExportMany(macros);
        var restored = MacroExporter.ImportMany(json);

        restored.Should().HaveCount(3);
        restored.Select(m => m.MacroId).Should().BeEquivalentTo(["m1", "m2", "m3"]);
        restored[0].DisplayName.Should().Be("Macro One");
        restored[1].DisplayName.Should().Be("Macro Two");
        restored[2].DisplayName.Should().Be("Macro Three");
    }

    [Fact]
    public void ImportFromJson_InvalidJson_Throws()
    {
        var invalidJson = "{ this is not valid json }}}";

        var act = () => MacroExporter.ImportFromJson(invalidJson);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ExportToJson_NullMacro_Throws()
    {
        var act = () => MacroExporter.ExportToJson(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
