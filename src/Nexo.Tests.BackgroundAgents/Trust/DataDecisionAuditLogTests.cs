using FluentAssertions;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

public class DataDecisionAuditLogTests
{
    [Fact]
    public void LogSanitization_AndGetRecent_ReturnsEntry()
    {
        var log = new DataDecisionAuditLog();
        var dto = new SanitizationAuditEntryDto(
            DateTimeOffset.UtcNow,
            "v1",
            "prompt",
            "redacted",
            "PII detected");

        log.LogSanitization(dto);

        var entries = log.GetRecent(10);
        entries.Should().HaveCount(1);
        entries[0].EventType.Should().Be("Sanitization");
        entries[0].RuleVersion.Should().Be("v1");
        entries[0].FieldOrType.Should().Be("prompt");
        entries[0].Disposition.Should().Be("redacted");
    }

    [Fact]
    public void LogRedaction_AsISanitizationAuditLog_StoresEntry()
    {
        var log = new DataDecisionAuditLog();
        log.LogRedaction(DateTimeOffset.UtcNow, "trust-v1", "prompt", "blocked", "PII");

        var entries = log.GetRecent(10);
        entries.Should().HaveCount(1);
        entries[0].EventType.Should().Be("Sanitization");
    }

    [Fact]
    public void LogBoundaryChange_StoresEntry()
    {
        var log = new DataDecisionAuditLog();
        log.LogBoundaryChange(new BoundaryChangeEvent
        {
            ChangeType = "pause",
            NewState = "paused",
            Timestamp = DateTimeOffset.UtcNow,
        });

        var entries = log.GetRecent(10);
        entries.Should().HaveCount(1);
        entries[0].EventType.Should().Be("BoundaryChange");
        entries[0].ChangeType.Should().Be("pause");
        entries[0].NewState.Should().Be("paused");
    }

    [Fact]
    public void LogClassification_StoresEntry()
    {
        var log = new DataDecisionAuditLog();
        log.LogClassification("file-paths", "TopSecret", "taxonomy default");

        var entries = log.GetRecent(10);
        entries.Should().HaveCount(1);
        entries[0].EventType.Should().Be("Classification");
        entries[0].DataType.Should().Be("file-paths");
        entries[0].LevelName.Should().Be("TopSecret");
    }

    [Fact]
    public void ExportToJson_ReturnsValidJson()
    {
        var log = new DataDecisionAuditLog();
        log.LogSanitization(new SanitizationAuditEntryDto(
            DateTimeOffset.UtcNow, "v1", "prompt", "redacted", "PII"));
        log.LogClassification("test", "Public", null);

        var json = log.ExportToJson(10);

        json.Should().Contain("Sanitization");
        json.Should().Contain("Classification");
        json.Should().Contain("exportedAt");
        System.Text.Json.JsonDocument.Parse(json);
    }

    [Fact]
    public void ExportToMarkdown_ReturnsMarkdown()
    {
        var log = new DataDecisionAuditLog();
        log.LogSanitization(new SanitizationAuditEntryDto(
            DateTimeOffset.UtcNow, "v1", "field", "stripped", "reason"));

        var md = log.ExportToMarkdown(10);

        md.Should().Contain("# Data Decision Audit Log");
        md.Should().Contain("Sanitization");
    }

    [Fact]
    public void GetRecent_WithSince_FiltersByTime()
    {
        var log = new DataDecisionAuditLog();
        log.LogClassification("a", "Public", null);

        var since = DateTimeOffset.UtcNow.AddMinutes(1);
        var entries = log.GetRecent(10, since);

        entries.Should().BeEmpty();
    }
}
