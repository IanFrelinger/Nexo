using FluentAssertions;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Skills.Models;
using Nexo.Infrastructure.Skills;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Skills;

public sealed class SkillAuditEmitterTests
{
    [Fact]
    public void Log_emits_skill_event_to_data_decision_audit_log()
    {
        var auditLog = new DataDecisionAuditLog();
        var emitter = new SkillAuditEmitter(dataDecisionAuditLog: auditLog);

        emitter.Log(new NexoSkillAuditEvent(
            NexoSkillAuditEventType.SkillAdvertised,
            "nexo-trust-inspector",
            "tester",
            "internal",
            "Trusted",
            "internal-only",
            "corr-1",
            DateTimeOffset.UtcNow,
            Detail: "advertised"));

        var entries = auditLog.GetRecent(10, eventType: NexoSkillAuditEventType.SkillAdvertised);
        entries.Should().ContainSingle();
        entries[0].SkillName.Should().Be("nexo-trust-inspector");
        entries[0].TrustTier.Should().Be("Trusted");
        entries[0].PolicyPackId.Should().Be("internal-only");
    }

    [Theory]
    [InlineData("SKILL_LOADED")]
    [InlineData("SKILL_RESOURCE_READ")]
    [InlineData("SKILL_SCRIPT_APPROVAL_REQUESTED")]
    [InlineData("SKILL_SCRIPT_APPROVAL_RESOLVED")]
    [InlineData("SKILL_SCRIPT_EXECUTED")]
    public void Log_records_each_disclosure_stage(string eventType)
    {
        var auditLog = new DataDecisionAuditLog();
        var emitter = new SkillAuditEmitter(dataDecisionAuditLog: auditLog);

        emitter.Log(new NexoSkillAuditEvent(
            eventType,
            "skill",
            "tester",
            "public",
            "Untrusted",
            "strict-enterprise",
            "corr-2",
            DateTimeOffset.UtcNow,
            ScriptPath: "scripts/a.sh",
            ScriptContentHash: "ABC",
            Outcome: "success",
            DurationMs: 12));

        auditLog.GetRecent(5, eventType: eventType).Should().ContainSingle();
    }
}
