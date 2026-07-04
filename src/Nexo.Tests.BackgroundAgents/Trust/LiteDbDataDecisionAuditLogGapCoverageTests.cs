using FluentAssertions;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

/// <summary>Tests for lite db data decision audit log gap coverage.</summary>
public sealed class LiteDbDataDecisionAuditLogGapCoverageTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"nexo_audit_gap_{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public void LogBoundaryChange_Classification_and_AmbientAction_round_trip()
    {
        var log = new LiteDbDataDecisionAuditLog(_dbPath);

        log.LogBoundaryChange(new BoundaryChangeEvent
        {
            ChangeType = "Enabled",
            Category = "file-paths",
            SourceId = "vscode",
            ProjectPath = "/repo",
            PreviousState = "Disabled",
            NewState = "Enabled",
            Timestamp = DateTimeOffset.UtcNow,
        });
        log.LogClassification("prompt", "Internal", "test");
        log.LogAmbientAction("agent-1", "did work", 3);

        var entries = log.GetRecent(10);
        entries.Should().Contain(e => e.EventType == "BoundaryChange");
        entries.Should().Contain(e => e.EventType == "Classification");
        entries.Should().Contain(e => e.EventType == "AmbientAction" && e.SourceId == "agent-1");
    }

    [Fact]
    public void LogScopeChainRejection_and_LogRedaction_persist()
    {
        var log = new LiteDbDataDecisionAuditLog(_dbPath);
        log.LogScopeChainRejection(["a", "b"], 2, "/tmp/path", "denied");
        log.LogRedaction(DateTimeOffset.UtcNow, "v2", "email", "redacted", "PII");

        var entries = log.GetRecent(10, eventType: "ScopeChainRejected");
        entries.Should().ContainSingle();
        log.GetRecent(10, eventType: "Sanitization").Should().ContainSingle();
    }

    [Fact]
    public void ExportToJson_and_ExportToMarkdown_include_logged_entries()
    {
        var log = new LiteDbDataDecisionAuditLog(_dbPath);
        log.LogClassification("secret", "Restricted", "manual");

        log.ExportToJson(maxCount: 5).Should().Contain("Classification");
        log.ExportToMarkdown(maxCount: 5).Should().Contain("secret");
    }

    [Fact]
    public void Buffer_flushes_after_threshold()
    {
        var log = new LiteDbDataDecisionAuditLog(_dbPath);
        for (var i = 0; i < 10; i++)
            log.LogClassification($"type-{i}", "Public", null);

        log.GetRecent(20).Should().HaveCount(10);
    }

    [Fact]
    public void GetRecent_filters_by_since_until_and_event_type()
    {
        var log = new LiteDbDataDecisionAuditLog(_dbPath);
        var now = DateTimeOffset.UtcNow;
        log.LogSanitization(new SanitizationAuditEntryDto(now.AddMinutes(-5), "v1", "field", "redacted", "r1"));
        log.LogClassification("x", "Public", null);

        log.GetRecent(10, since: now.AddMinutes(-1), eventType: "Classification")
            .Should().ContainSingle(e => e.EventType == "Classification");
    }
}
