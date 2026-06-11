using FluentAssertions;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

public sealed class LiteDbDataDecisionAuditLogTests : IDisposable
{
    private readonly string _dbPath;

    public LiteDbDataDecisionAuditLogTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"nexo_audit_test_{Guid.NewGuid():N}.db");
    }

    public void Dispose() => Dispose(true);
    private void Dispose(bool disposing)
    {
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public void Ctor_WithPath_CreatesInstance()
    {
        var log = new LiteDbDataDecisionAuditLog(_dbPath);
        log.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_NullOrEmpty_ThrowsArgumentNullException()
    {
        var act = () => new LiteDbDataDecisionAuditLog("");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LogSanitization_AndGetRecent_ReturnsEntry()
    {
        var log = new LiteDbDataDecisionAuditLog(_dbPath);
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
    }

    [Fact]
    public void GetRecent_PersistsAcrossInstances()
    {
        var log1 = new LiteDbDataDecisionAuditLog(_dbPath);
        log1.LogSanitization(new SanitizationAuditEntryDto(
            DateTimeOffset.UtcNow, "v1", "field", "stripped", "reason"));

        _ = log1.GetRecent(10);

        var log2 = new LiteDbDataDecisionAuditLog(_dbPath);
        var entries = log2.GetRecent(10);

        entries.Should().HaveCount(1);
        entries[0].EventType.Should().Be("Sanitization");
    }

    [Fact]
    public void ExportToCsv_ReturnsCsvWithHeader()
    {
        var log = new LiteDbDataDecisionAuditLog(_dbPath);
        log.LogSanitization(new SanitizationAuditEntryDto(
            DateTimeOffset.UtcNow, "v1", "field", "stripped", "reason"));

        var csv = log.ExportToCsv(10);

        csv.Should().Contain("Timestamp,EventType,TenantId,RuleVersion,FieldOrType,Disposition");
        csv.Should().Contain("Sanitization");
    }
}
