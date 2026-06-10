using FluentAssertions;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Xunit;

namespace Nexo.Tests.Contracts;

/// <summary>
/// Contract tests for IDataDecisionAuditLog implementations.
/// Every implementation must pass these tests to ensure consistent behavior.
/// </summary>
public abstract class DataDecisionAuditLogContractTests
{
    protected abstract IDataDecisionAuditLog CreateInstance();

    [Fact]
    public void LogScopeChainRejection_IsAlwaysRetrievable()
    {
        var log = CreateInstance();
        var chainIds = new[] { "0:repo.fs.write:src/a.cs", "1:repo.fs.write:.git/config" };
        log.LogScopeChainRejection(chainIds, 1, ".git/config", "Path not allowed");

        var recent = log.GetRecent(10, eventType: "ScopeChainRejected");
        recent.Should().ContainSingle();
        recent[0].EventType.Should().Be("ScopeChainRejected");
        recent[0].ProjectPath.Should().Be(".git/config");
        recent[0].Reason.Should().Contain("Path not allowed");
    }

    [Fact]
    public void NoEventIsLostUnderLoad()
    {
        var log = CreateInstance();
        var count = 15;
        for (var i = 0; i < count; i++)
        {
            log.LogScopeChainRejection(
                new[] { $"{i}:repo.fs.write:src/file{i}.cs" },
                0,
                $"src/file{i}.cs",
                $"reason-{i}");
        }

        var recent = log.GetRecent(count * 2, eventType: "ScopeChainRejected");
        recent.Should().HaveCount(count);
    }

    [Fact]
    public void ExportToJson_ContainsAllEvents()
    {
        var log = CreateInstance();
        log.LogScopeChainRejection(new[] { "0:repo.fs.write:src/a.cs" }, 0, "src/a.cs", "Path not allowed");
        log.LogAmbientAction("agent-1", "summary", 3);

        var json = log.ExportToJson();

        json.Should().Contain("ScopeChainRejected");
        json.Should().Contain("AmbientAction");
    }

    [Fact]
    public void LogCopilotTask_IsTenantScopedAndRetrievable()
    {
        var log = CreateInstance();
        log.LogCopilotTask("tenant-a", "task-123", success: true);

        var recent = log.GetRecent(10, eventType: "CopilotTask");
        recent.Should().ContainSingle();
        recent[0].TenantId.Should().Be("tenant-a");
        recent[0].SourceId.Should().Be("task-123");
        recent[0].Disposition.Should().Be("Success");
    }
}
