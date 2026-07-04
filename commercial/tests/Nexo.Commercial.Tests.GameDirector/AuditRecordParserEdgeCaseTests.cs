using FluentAssertions;
using GameDirector.Domain;
using Nexo.Core.Application.Trust.Models;
using Xunit;

namespace Nexo.Tests.GameDirector;

/// <summary>Tests for audit record parser edge case.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class AuditRecordParserEdgeCaseTests
{
    [Fact]
    public void TryParse_rejects_empty_reason()
    {
        var entry = new DataDecisionAuditEntry
        {
            EventType = "Classification",
            DataType = "game-director",
            Reason = "",
            Timestamp = DateTimeOffset.UtcNow
        };

        AuditRecordParser.TryParse(entry, out var record).Should().BeFalse();
        record.Should().BeNull();
    }

    [Fact]
    public void TryParse_rejects_malformed_json_reason()
    {
        var entry = new DataDecisionAuditEntry
        {
            EventType = "Classification",
            DataType = "game-director",
            Reason = "{not valid",
            Timestamp = DateTimeOffset.UtcNow,
            LevelName = "fallback"
        };

        AuditRecordParser.TryParse(entry, out var record).Should().BeFalse();
        record.Should().BeNull();
    }

    [Fact]
    public void TryParse_uses_level_name_when_tool_missing()
    {
        var entry = new DataDecisionAuditEntry
        {
            EventType = "Classification",
            DataType = "game-director",
            LevelName = "fallback-tool",
            Reason = "{}",
            Timestamp = DateTimeOffset.UtcNow
        };

        AuditRecordParser.TryParse(entry, out var record).Should().BeTrue();
        record!.Tool.Should().Be("fallback-tool");
        record.ModelId.Should().Be("deterministic");
        record.TrustPolicy.Should().Be("unknown");
        record.SanitizationEvents.Should().BeEmpty();
    }
}
