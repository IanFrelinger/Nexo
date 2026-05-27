using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

public sealed class NoOpBarrierAuditSinkGapCoverageTests
{
    [Fact]
    public async Task WriteAsync_completes_without_writing()
    {
        var sink = new NoOpBarrierAuditSink();

        var act = async () => await sink.WriteAsync(new BarrierAuditEvent(
            BarrierAuditEventType.AgentInvoked,
            "public",
            BarrierAuthoritySource.Cli,
            AgentName: "agent",
            CorrelationId: "corr",
            SpanId: "span",
            DateTimeOffset.UtcNow));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WriteAsync_accepts_null_event()
    {
        var sink = new NoOpBarrierAuditSink();

        var act = async () => await sink.WriteAsync(null!, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
