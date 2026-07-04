using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Tests for no op barrier audit sink.</summary>
public sealed class NoOpBarrierAuditSinkTests
{
    [Fact]
    public async Task WriteAsync_ReturnsCompleted_AndDoesNotThrow()
    {
        var sink = new NoOpBarrierAuditSink();
        var auditEvent = new BarrierAuditEvent(
            BarrierAuditEventType.AgentInvoked,
            "internal",
            BarrierAuthoritySource.Cli,
            "agent-1",
            "corr-1",
            "span-1",
            DateTimeOffset.UtcNow,
            Detail: null);

        sink.WriteAsync(auditEvent).IsCompletedSuccessfully.Should().BeTrue();

        var act = async () => await sink.WriteAsync(auditEvent);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WriteAsync_AcceptsNullEvent()
    {
        var sink = new NoOpBarrierAuditSink();

        var act = async () => await sink.WriteAsync(null!, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
