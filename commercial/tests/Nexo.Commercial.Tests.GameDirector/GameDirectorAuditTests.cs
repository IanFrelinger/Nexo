using FluentAssertions;
using GameDirector.Domain;
using Nexo.BackgroundAgents.Trust;
using Xunit;

namespace Nexo.Tests.GameDirector;

/// <summary>Tests for game director audit.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class GameDirectorAuditTests
{
    [Fact]
    public void NewAuditId_returns_unique_non_empty_ids()
    {
        var a = GameDirectorAudit.NewAuditId();
        var b = GameDirectorAudit.NewAuditId();
        a.Should().NotBeNullOrWhiteSpace();
        b.Should().NotBe(a);
    }

    [Fact]
    public void HashInput_is_deterministic_for_same_payload()
    {
        var payload = new { sheet = "a", n = 3 };
        GameDirectorAudit.HashInput(payload).Should().Be(GameDirectorAudit.HashInput(payload));
    }

    [Fact]
    public void HashInput_differs_for_different_payloads()
    {
        GameDirectorAudit.HashInput(new { x = 1 }).Should().NotBe(GameDirectorAudit.HashInput(new { x = 2 }));
    }

    [Fact]
    public void Query_filters_by_tool_name()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        GameDirectorAudit.LogToolExecution(audit, "a1", "analyze_balance", "h1", "det", "internal-only", "one");
        GameDirectorAudit.LogToolExecution(audit, "a2", "validate_map", "h2", "local", "internal-only", "two");

        var balanceOnly = AuditRecordParser.Query(
            audit,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(5),
            tool: "analyze_balance");

        balanceOnly.Should().ContainSingle(r => r.Tool == "analyze_balance");
    }

    [Fact]
    public void Query_filters_by_asset_id_in_output_summary()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        GameDirectorAudit.LogToolExecution(audit, "x1", "validate_map", "h", "local", "pack", "map=arena_alpha equity=0.9");
        GameDirectorAudit.LogToolExecution(audit, "x2", "validate_map", "h", "local", "pack", "map=other equity=0.5");

        var filtered = AuditRecordParser.Query(
            audit,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(5),
            assetId: "arena_alpha");

        filtered.Should().ContainSingle().Which.OutputSummary.Should().Contain("arena_alpha");
    }

    [Fact]
    public void TryParse_ignores_non_game_director_entries()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        audit.LogClassification("other-type", "something", "{}");

        GameDirectorAudit.LogToolExecution(audit, "gd1", "generate_content", "h", "m", "p", "ok");

        var records = AuditRecordParser.Query(
            audit,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(1));

        records.Should().ContainSingle(r => r.AuditId == "gd1");
    }
}
