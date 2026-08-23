using FluentAssertions;
using Ashlar.BackgroundAgents.DataSensitivity;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Trust;

/// <summary>Tests for data taxonomy.</summary>
public class DataTaxonomyTests
{
    [Fact]
    public void GetDefaultLevelForDataType_KnownType_ReturnsLevel()
    {
        var taxonomy = new DataTaxonomy(new Dictionary<string, string?>
        {
            ["file-paths"] = "TopSecret",
            ["editor-events"] = "Public",
        });

        taxonomy.GetDefaultLevelForDataType("file-paths").Should().Be("TopSecret");
        taxonomy.GetDefaultLevelForDataType("editor-events").Should().Be("Public");
    }

    [Fact]
    public void GetDefaultLevelForDataType_UnknownType_FallsBackToTopSecret()
    {
        var taxonomy = new DataTaxonomy(new Dictionary<string, string?>());

        taxonomy.GetDefaultLevelForDataType("unknown-type").Should().Be("TopSecret");
    }

    [Fact]
    public void GetDefaultLevelForDataType_WithNullValue_ReturnsTopSecret()
    {
        var taxonomy = new DataTaxonomy(new Dictionary<string, string?>
        {
            ["inferred-preferences"] = null,
        });

        taxonomy.GetDefaultLevelForDataType("inferred-preferences").Should().Be("TopSecret");
    }

    [Fact]
    public void GetDefaultLevelForDataType_EmptyOrWhitespace_ReturnsTopSecret()
    {
        var taxonomy = new DataTaxonomy(new Dictionary<string, string?>());

        taxonomy.GetDefaultLevelForDataType("").Should().Be("TopSecret");
        taxonomy.GetDefaultLevelForDataType("   ").Should().Be("TopSecret");
    }

    [Fact]
    public void GetDefaultLevelForDataType_CaseInsensitive_ResolvesCorrectly()
    {
        var taxonomy = new DataTaxonomy(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["file-contents"] = "TopSecret",
        });

        taxonomy.GetDefaultLevelForDataType("FILE-CONTENTS").Should().Be("TopSecret");
    }
}
