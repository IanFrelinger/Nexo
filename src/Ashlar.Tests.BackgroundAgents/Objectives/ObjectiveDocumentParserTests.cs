using FluentAssertions;
using Ashlar.BackgroundAgents.Objectives;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Objectives;

/// <summary>Tests for objective document parser.</summary>
public class ObjectiveDocumentParserTests
{
    [Fact]
    public void Parses_minimal_frontmatter_and_body()
    {
        var text = """
            ---
            id: improve-telemetry
            title: Improve telemetry coverage
            status: Pending
            priority: 10
            ---

            Wire the planner into the cycles.jsonl tail so we can…
            """;

        var doc = ObjectiveDocumentParser.Parse(text, fallbackId: "ignored");
        doc.Id.Should().Be("improve-telemetry");
        doc.Title.Should().Be("Improve telemetry coverage");
        doc.Status.Should().Be(ObjectiveStatus.Pending);
        doc.Priority.Should().Be(10);
        doc.Body.Should().StartWith("Wire the planner");
    }

    [Fact]
    public void Falls_back_when_frontmatter_absent()
    {
        var text = "Just markdown body, no frontmatter.";
        var doc = ObjectiveDocumentParser.Parse(text, fallbackId: "loose-note", fallbackStatus: ObjectiveStatus.Pending);

        doc.Id.Should().Be("loose-note");
        doc.Title.Should().Be("loose-note");
        doc.Status.Should().Be(ObjectiveStatus.Pending);
        doc.Body.Should().Be("Just markdown body, no frontmatter.");
    }

    [Fact]
    public void Parses_quoted_strings_and_tag_list()
    {
        var text = """
            ---
            id: needs-quoting
            title: "Improve: telemetry"
            tags: [build, test, "needs-care"]
            owner: runtime-planner
            attempts: 3
            blocked_reason: "stuck waiting for ollama"
            status: Blocked
            ---
            body
            """;

        var doc = ObjectiveDocumentParser.Parse(text, "fallback");
        doc.Title.Should().Be("Improve: telemetry");
        doc.Tags.Should().Equal("build", "test", "needs-care");
        doc.Owner.Should().Be("runtime-planner");
        doc.Attempts.Should().Be(3);
        doc.BlockedReason.Should().Be("stuck waiting for ollama");
        doc.Status.Should().Be(ObjectiveStatus.Blocked);
    }

    [Fact]
    public void Round_trips_via_serialize_then_parse()
    {
        var original = new ObjectiveDocument
        {
            Id = "rt-1",
            Title = "Round trip",
            Status = ObjectiveStatus.InProgress,
            Owner = "runtime-planner",
            Priority = 1,
            Attempts = 2,
            Tags = new[] { "alpha", "beta" },
            CreatedAt = DateTimeOffset.Parse("2026-04-18T12:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-04-18T12:30:00Z"),
            Body = "First body line.\n\nSecond paragraph."
        };

        var serialized = original.Serialize();
        var roundTripped = ObjectiveDocumentParser.Parse(serialized, "ignored");

        roundTripped.Id.Should().Be(original.Id);
        roundTripped.Title.Should().Be(original.Title);
        roundTripped.Status.Should().Be(original.Status);
        roundTripped.Owner.Should().Be(original.Owner);
        roundTripped.Priority.Should().Be(original.Priority);
        roundTripped.Attempts.Should().Be(original.Attempts);
        roundTripped.Tags.Should().Equal(original.Tags);
        roundTripped.CreatedAt.Should().Be(original.CreatedAt);
        roundTripped.UpdatedAt.Should().Be(original.UpdatedAt);
        roundTripped.Body.Should().Be(original.Body);
    }

    [Fact]
    public void Unknown_keys_and_blank_lines_are_ignored()
    {
        var text = """
            ---
            id: x
            title: Y
            unknown_field: whatever
            # this is a comment
            
            priority: 7
            ---
            body
            """;
        var doc = ObjectiveDocumentParser.Parse(text, "x");
        doc.Priority.Should().Be(7);
        doc.Title.Should().Be("Y");
    }

    [Fact]
    public void Defaults_apply_to_missing_fields()
    {
        var text = """
            ---
            id: bare
            title: Bare objective
            ---
            """;
        var doc = ObjectiveDocumentParser.Parse(text, "bare");
        doc.Status.Should().Be(ObjectiveStatus.Pending);
        doc.Priority.Should().Be(100);
        doc.Attempts.Should().Be(0);
        doc.Tags.Should().BeEmpty();
        doc.Owner.Should().BeNull();
        doc.Body.Should().BeEmpty();
    }
}
