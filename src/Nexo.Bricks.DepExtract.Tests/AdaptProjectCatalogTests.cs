using FluentAssertions;
using Nexo.Bricks.DepExtract;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class AdaptProjectCatalogTests
{
    static string FixturesRoot =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "e2e", "projects");

    [Theory]
    [InlineData("P01_SimpleTickPull", "SimpleTickStream.hpp", true)]
    [InlineData("P02_NextEventNamed", "SensorTap.hpp", true)]
    [InlineData("P03_DualHeaderBody", "FlightBodyReader.hpp", true)]
    [InlineData("P04_LegacyRadar", "LegacyRadarLog.hpp", false)]
    [InlineData("P05_SessionFactory", "SessionFactory.hpp", false)]
    [InlineData("P06_ChunkedCursor", "ChunkCursor.hpp", false)]
    [InlineData("P07_EvtqTwin", "TwinEvtqStream.hpp", true)]
    [InlineData("P08_NoPullSurface", "ConfigBlob.hpp", false)]
    [InlineData("P09_Path With Spaces", "TickSpaceStream.hpp", true)]
    [InlineData("P10_NeedsCompanion", "StreamWithCpp.hpp", true)]
    public void Catalog_entry_matches_scaffold_expectation(string projectId, string entry, bool scaffoldable)
    {
        var path = Path.Combine(FixturesRoot, projectId, entry);
        File.Exists(path).Should().BeTrue(path);
        var src = File.ReadAllText(path);
        // Include companions in the same folder for dual-class / multi-file surfaces.
        foreach (var extra in Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.hpp")
                     .Where(p => !string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
            src += "\n" + File.ReadAllText(extra);

        AdapterScaffold.LooksSimple(src).Should().Be(scaffoldable, projectId);
        if (scaffoldable)
        {
            var code = AdapterScaffold.TryBuild(src, entry);
            code.Should().NotBeNullOrWhiteSpace();
            code.Should().Contain("CustomEventReader");
        }
    }

    [Fact]
    public void Catalog_json_lists_expected_projects()
    {
        var catalogPath = Path.Combine(FixturesRoot, "catalog.json");
        File.Exists(catalogPath).Should().BeTrue();
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(catalogPath));
        var projects = doc.RootElement.GetProperty("projects").EnumerateArray().ToArray();
        projects.Length.Should().BeGreaterThanOrEqualTo(10);
        projects.Select(p => p.GetProperty("id").GetString()).Should()
            .Contain(new[]
            {
                "P01_SimpleTickPull", "P04_LegacyRadar", "P07_EvtqTwin",
                "P08_NoPullSurface", "P09_Path With Spaces", "P10_NeedsCompanion",
            });
    }
}
