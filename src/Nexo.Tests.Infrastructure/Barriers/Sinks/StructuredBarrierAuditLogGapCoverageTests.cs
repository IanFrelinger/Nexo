using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime.Barriers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Tests for structured barrier audit log gap coverage.</summary>
public sealed class StructuredBarrierAuditLogGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_logger()
    {
        var act = () => new StructuredBarrierAuditLog(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_null_sinks_defaults_to_empty_and_warns_once()
    {
        var logger = new TestLogger<StructuredBarrierAuditLog>();

        _ = new StructuredBarrierAuditLog(logger, sinks: null);

        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("No IBarrierAuditSink registered", StringComparison.Ordinal));
    }

    [Fact]
    public void Constructor_only_noop_sinks_emits_discard_warning()
    {
        var logger = new TestLogger<StructuredBarrierAuditLog>();

        _ = new StructuredBarrierAuditLog(logger, [new NoOpBarrierAuditSink(), new NoOpBarrierAuditSink()]);

        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("No IBarrierAuditSink registered", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RecordAsync_throws_for_null_event()
    {
        var sut = new StructuredBarrierAuditLog(new TestLogger<StructuredBarrierAuditLog>(), [new CapturingSink()]);

        var act = async () => await sut.RecordAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("auditEvent");
    }

    [Fact]
    public async Task RecordAsync_mixed_noop_and_real_sink_writes_only_real_sink()
    {
        var logger = new TestLogger<StructuredBarrierAuditLog>();
        var capturing = new CapturingSink();
        var sut = new StructuredBarrierAuditLog(logger, [new NoOpBarrierAuditSink(), capturing]);
        var auditEvent = CreateEvent();

        await sut.RecordAsync(auditEvent, CancellationToken.None);

        capturing.Events.Should().ContainSingle().Which.Should().BeSameAs(auditEvent);
        logger.Entries.Should().BeEmpty("noop-only check requires all sinks to be NoOp");
    }

    [Fact]
    public async Task RecordAsync_logs_warning_when_sink_write_throws()
    {
        var logger = new TestLogger<StructuredBarrierAuditLog>();
        var sut = new StructuredBarrierAuditLog(logger, [new ThrowingSink()]);
        var auditEvent = CreateEvent();

        await sut.RecordAsync(auditEvent, CancellationToken.None);

        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("Barrier audit sink failure", StringComparison.Ordinal));
        logger.Entries[0].Exception.Should().NotBeNull();
    }

    private static BarrierAuditEvent CreateEvent()
        => new(
            BarrierAuditEventType.ContextInitialized,
            "public",
            BarrierAuthoritySource.Header,
            AgentName: string.Empty,
            CorrelationId: "corr-gap",
            SpanId: string.Empty,
            DateTimeOffset.UtcNow,
            Detail: "test");

    /// <summary>Capturing sink.</summary>
    private sealed class CapturingSink : IBarrierAuditSink
    {
        /// <summary>Events.</summary>
        public List<BarrierAuditEvent> Events { get; } = [];

        public ValueTask WriteAsync(BarrierAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Throwing sink.</summary>
    private sealed class ThrowingSink : IBarrierAuditSink
    {
        public ValueTask WriteAsync(BarrierAuditEvent auditEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("sink write failed");
    }
}
