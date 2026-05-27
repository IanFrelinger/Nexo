using System.Text.Json;
using FluentAssertions;
using Nexo.Core.Application.Trust.Models;
using Nexo.Infrastructure.Trust;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

public sealed class UserKnowledgeLogExportHelperGapCoverageTests
{
    [Fact]
    public void ToJson_serializes_empty_collection()
    {
        var json = UserKnowledgeLogExportHelper.ToJson([]);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("count").GetInt32().Should().Be(0);
        document.RootElement.GetProperty("entries").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void ToJson_includes_entry_fields_and_provenance()
    {
        var entry = new UserKnowledgeLogEntry
        {
            Id = "entry-1",
            DataType = "inferred-preferences",
            Content = "prefers dark mode",
            SourceObservationIds = ["obs-1", "obs-2"],
            Version = 2,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
        };

        var json = UserKnowledgeLogExportHelper.ToJson([entry]);

        using var document = JsonDocument.Parse(json);
        var exported = document.RootElement.GetProperty("entries")[0];
        exported.GetProperty("Id").GetString().Should().Be("entry-1");
        exported.GetProperty("DataType").GetString().Should().Be("inferred-preferences");
        exported.GetProperty("Content").GetString().Should().Be("prefers dark mode");
        exported.GetProperty("SourceObservationIds").GetArrayLength().Should().Be(2);
        exported.GetProperty("Version").GetInt32().Should().Be(2);
    }

    [Fact]
    public void ToMarkdown_includes_header_and_entry_details()
    {
        var entry = new UserKnowledgeLogEntry
        {
            Id = "entry-md",
            DataType = "behavioral-patterns",
            Content = "Runs tests before commit",
            SourceObservationIds = ["obs-a"],
            Version = 1,
            CreatedAt = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 2, 2, 12, 0, 0, TimeSpan.Zero),
        };

        var markdown = UserKnowledgeLogExportHelper.ToMarkdown([entry]);

        markdown.Should().Contain("# Nexo User Knowledge Log");
        markdown.Should().Contain("## entry-md");
        markdown.Should().Contain("**DataType:** behavioral-patterns");
        markdown.Should().Contain("**Provenance:**");
        markdown.Should().Contain("- obs-a");
        markdown.Should().Contain("Runs tests before commit");
    }

    [Fact]
    public void ToMarkdown_omits_provenance_section_when_empty()
    {
        var entry = new UserKnowledgeLogEntry
        {
            Id = "entry-no-prov",
            DataType = "notes",
            Content = "plain note",
        };

        var markdown = UserKnowledgeLogExportHelper.ToMarkdown([entry]);

        markdown.Should().NotContain("**Provenance:**");
        markdown.Should().Contain("plain note");
    }
}
