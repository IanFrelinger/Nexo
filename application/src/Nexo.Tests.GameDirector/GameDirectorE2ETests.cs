using FluentAssertions;
using GameDirector.Bricks;
using GameDirector.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.GameDirector;

[Trait("Category", "GameDirectorE2E")]
public sealed class GameDirectorE2ETests
{
    [Fact]
    public async Task BalanceAnalysisBrick_returns_audit_id_and_flags_drift()
    {
        var audit = new LiteDbDataDecisionAuditLog(Path.Combine(Path.GetTempPath(), $"gd-audit-{Guid.NewGuid():N}.db"));
        var brick = new BalanceAnalysisBrick(audit, NullLogger<BalanceAnalysisBrick>.Instance);

        var input = new BrickInput();
        input.Set("sheet_id", "halo_weapons_v1");
        input.Set("data", System.Text.Json.JsonSerializer.SerializeToElement(new[]
        {
            new { id = "assault_rifle", stats = new { damage = 12.0, fire_rate = 600.0, ttk_ms = 920.0 } }
        }));

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            new Nexo.Infrastructure.Execution.ExecutionContext(),
            CancellationToken.None);

        output.Get<string>("audit_id").Should().NotBeNullOrWhiteSpace();
        output.ToDictionary().Should().ContainKey("flagged");
        var flaggedObj = output.ToDictionary()["flagged"];
        flaggedObj.Should().NotBeNull();
    }

    [Fact]
    public void AuditRecordParser_round_trips_logged_entry()
    {
        var audit = new LiteDbDataDecisionAuditLog(Path.Combine(Path.GetTempPath(), $"gd-audit-{Guid.NewGuid():N}.db"));
        GameDirectorAudit.LogToolExecution(
            audit,
            "audit-123",
            "analyze_balance",
            "abc",
            "deterministic",
            "internal-only",
            "sheet=test flagged=1");

        var records = AuditRecordParser.Query(
            audit,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(1),
            tool: "analyze_balance");

        records.Should().ContainSingle(r => r.AuditId == "audit-123");
    }
}
