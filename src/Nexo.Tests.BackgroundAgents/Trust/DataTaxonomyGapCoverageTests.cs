using FluentAssertions;
using Nexo.BackgroundAgents.DataSensitivity;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

[Collection("DataTaxonomyGap")]
public class DataTaxonomyGapCoverageTests : IDisposable
{
    private readonly string _taxonomyPath;
    private readonly string? _backupPath;
    private readonly bool _hadOriginalFile;

    public DataTaxonomyGapCoverageTests()
    {
        _taxonomyPath = Path.Combine(AppContext.BaseDirectory, "DataTaxonomy.json");
        _hadOriginalFile = File.Exists(_taxonomyPath);
        if (_hadOriginalFile)
        {
            _backupPath = _taxonomyPath + ".bak-" + Guid.NewGuid().ToString("N");
            File.Move(_taxonomyPath, _backupPath);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_taxonomyPath))
            File.Delete(_taxonomyPath);

        if (_hadOriginalFile && _backupPath is not null)
            File.Move(_backupPath, _taxonomyPath);
    }

    [Fact]
    public void Default_constructor_returns_empty_mappings_when_file_has_no_mappings_property()
    {
        WriteTaxonomyFile("""
            {
              "version": 1
            }
            """);

        var taxonomy = new DataTaxonomy();

        taxonomy.GetDefaultLevelForDataType("custom-type").Should().Be("TopSecret");
    }

    [Fact]
    public void Default_constructor_loads_mappings_from_file_when_present()
    {
        WriteTaxonomyFile("""
            {
              "mappings": {
                "custom-type": "Confidential",
                "inherit-type": null
              }
            }
            """);

        var taxonomy = new DataTaxonomy();

        taxonomy.GetDefaultLevelForDataType("custom-type").Should().Be("Confidential");
        taxonomy.GetDefaultLevelForDataType("inherit-type").Should().Be("TopSecret");
    }

    [Fact]
    public void Default_constructor_falls_back_to_built_in_mappings_when_file_invalid()
    {
        WriteTaxonomyFile("{ not valid json");

        var taxonomy = new DataTaxonomy();

        taxonomy.GetDefaultLevelForDataType("api-keys").Should().Be("Secret");
        taxonomy.GetDefaultLevelForDataType("unknown").Should().Be("TopSecret");
    }

    [Fact]
    public void Default_constructor_uses_built_in_mappings_when_no_file_exists()
    {
        ClearTaxonomyCache();

        var taxonomy = new DataTaxonomy();

        taxonomy.GetDefaultLevelForDataType("process-names").Should().Be("Public");
        taxonomy.GetDefaultLevelForDataType("behavioral-patterns").Should().Be("TopSecret");
    }

    private void WriteTaxonomyFile(string content)
    {
        File.WriteAllText(_taxonomyPath, content);
        ClearTaxonomyCache();
    }

    private static void ClearTaxonomyCache()
    {
        var cacheField = typeof(DataTaxonomy).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic)!;
        var cache = (ConcurrentDictionary<string, IReadOnlyDictionary<string, string?>>)cacheField.GetValue(null)!;
        cache.Clear();
    }
}

[CollectionDefinition("DataTaxonomyGap", DisableParallelization = true)]
public sealed class DataTaxonomyGapCollection;
