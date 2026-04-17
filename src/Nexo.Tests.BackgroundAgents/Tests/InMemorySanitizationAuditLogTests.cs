using FluentAssertions;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Domain;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Tests;

public sealed class InMemorySanitizationAuditLogTests
{
    [Fact(Timeout = 15000)]
    public void LogRedaction_StoresEntry()
    {
        var log = new InMemorySanitizationAuditLog();
        var now = DateTimeOffset.UtcNow;

        log.LogRedaction(now, "v1", "prompt", "redacted", "PII detected");

        var entries = log.GetRecent(10);
        entries.Should().HaveCount(1);
        entries[0].Timestamp.Should().Be(now);
        entries[0].RuleVersion.Should().Be("v1");
        entries[0].FieldOrType.Should().Be("prompt");
        entries[0].Disposition.Should().Be("redacted");
        entries[0].Reason.Should().Be("PII detected");
    }

    [Fact(Timeout = 15000)]
    public void GetRecent_FiltersBySince()
    {
        var log = new InMemorySanitizationAuditLog();
        var old = DateTimeOffset.UtcNow.AddHours(-2);
        var recent = DateTimeOffset.UtcNow;

        log.LogRedaction(old, "v1", "field-old", "stripped", null);
        log.LogRedaction(recent, "v1", "field-new", "blocked", "reason");

        var cutoff = DateTimeOffset.UtcNow.AddHours(-1);
        var entries = log.GetRecent(10, since: cutoff);

        entries.Should().HaveCount(1);
        entries[0].FieldOrType.Should().Be("field-new");
    }

    [Fact(Timeout = 15000)]
    public void MaxEntries_EnforcesLimit()
    {
        var log = new InMemorySanitizationAuditLog();
        var max = NexoDefaults.SanitizationAuditMaxEntries;

        for (var i = 0; i < max + 500; i++)
        {
            log.LogRedaction(DateTimeOffset.UtcNow, "v1", $"field-{i}", "stripped", null);
        }

        var entries = log.GetRecent(max + 1000);
        entries.Should().HaveCount(max);
    }

    [Fact(Timeout = 15000)]
    public void GetRecent_OrdersByTimestampDescending()
    {
        var log = new InMemorySanitizationAuditLog();
        var t1 = DateTimeOffset.UtcNow.AddMinutes(-3);
        var t2 = DateTimeOffset.UtcNow.AddMinutes(-1);
        var t3 = DateTimeOffset.UtcNow;

        log.LogRedaction(t2, "v1", "middle", "stripped", null);
        log.LogRedaction(t1, "v1", "oldest", "stripped", null);
        log.LogRedaction(t3, "v1", "newest", "stripped", null);

        var entries = log.GetRecent(10);

        entries.Should().HaveCount(3);
        entries[0].FieldOrType.Should().Be("newest");
        entries[1].FieldOrType.Should().Be("middle");
        entries[2].FieldOrType.Should().Be("oldest");
    }
}
