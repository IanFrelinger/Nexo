using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Tests for structured log barrier audit sink.</summary>
public sealed class StructuredLogBarrierAuditSinkTests
{
    [Theory]
    [InlineData("BARRIER_INITIALIZED", LogLevel.Information)]
    [InlineData("BARRIER_IDENTITY_RESOLVED", LogLevel.Information)]
    [InlineData("BARRIER_AGENT_INVOKED", LogLevel.Information)]
    [InlineData("BARRIER_DEFAULT_APPLIED", LogLevel.Warning)]
    [InlineData("BARRIER_WIRE_PROPAGATED", LogLevel.Debug)]
    [InlineData("BARRIER_VALIDATION_FAILED", LogLevel.Warning)]
    [InlineData("BARRIER_CEILING_EXCEEDED", LogLevel.Error)]
    [InlineData("BARRIER_ELEVATION_ATTEMPTED", LogLevel.Error)]
    [InlineData("BARRIER_ELEVATION_DENIED", LogLevel.Error)]
    public async Task WriteAsync_UsesDefaultLogLevelPerEventType(string eventType, LogLevel expectedLevel)
    {
        var logger = new TestLogger<StructuredLogBarrierAuditSink>();
        var sink = new StructuredLogBarrierAuditSink(logger, new StructuredLogBarrierAuditSinkOptions());

        await sink.WriteAsync(CreateEvent(eventType));

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(expectedLevel);
    }

    [Fact]
    public async Task WriteAsync_RespectsEventLevelOverrides()
    {
        var logger = new TestLogger<StructuredLogBarrierAuditSink>();
        var sink = new StructuredLogBarrierAuditSink(
            logger,
            new StructuredLogBarrierAuditSinkOptions
            {
                EventLevelOverrides = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase)
                {
                    [BarrierAuditEventType.ElevationDenied] = LogLevel.Critical
                }
            });

        await sink.WriteAsync(CreateEvent(BarrierAuditEventType.ElevationDenied));

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Critical);
    }

    [Fact]
    public async Task WriteAsync_UnknownEventType_DefaultsToWarning()
    {
        var logger = new TestLogger<StructuredLogBarrierAuditSink>();
        var sink = new StructuredLogBarrierAuditSink(logger, new StructuredLogBarrierAuditSinkOptions());

        await sink.WriteAsync(CreateEvent("SOMETHING_NEW"));

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task WriteAsync_WritesAllAuditFieldsAsNamedProperties()
    {
        var logger = new TestLogger<StructuredLogBarrierAuditSink>();
        var sink = new StructuredLogBarrierAuditSink(logger, new StructuredLogBarrierAuditSinkOptions());
        var auditEvent = new BarrierAuditEvent(
            EventType: BarrierAuditEventType.AgentInvoked,
            BarrierLevel: "internal",
            AuthoritySource: BarrierAuthoritySource.Cli,
            AgentName: "CodeGenerationAgent",
            CorrelationId: "corr-123",
            SpanId: "span-456",
            OccurredAt: DateTimeOffset.UtcNow,
            Detail: "detail-text");

        await sink.WriteAsync(auditEvent);

        logger.Entries.Should().ContainSingle();
        var props = logger.Entries[0].Properties;
        props.Should().ContainKey("EventType").WhoseValue.Should().Be(auditEvent.EventType);
        props.Should().ContainKey("BarrierLevel").WhoseValue.Should().Be(auditEvent.BarrierLevel);
        props.Should().ContainKey("AuthoritySource").WhoseValue.Should().Be(auditEvent.AuthoritySource);
        props.Should().ContainKey("AgentName").WhoseValue.Should().Be(auditEvent.AgentName);
        props.Should().ContainKey("CorrelationId").WhoseValue.Should().Be(auditEvent.CorrelationId);
        props.Should().ContainKey("SpanId").WhoseValue.Should().Be(auditEvent.SpanId);
        props.Should().ContainKey("Detail").WhoseValue.Should().Be(auditEvent.Detail);
        props.Should().ContainKey("OccurredAt");
    }

    [Fact]
    public async Task WriteAsync_NeverThrows_WhenLoggerFails()
    {
        var sink = new StructuredLogBarrierAuditSink(
            new ThrowingLogger<StructuredLogBarrierAuditSink>(),
            new StructuredLogBarrierAuditSinkOptions());

        var act = async () => await sink.WriteAsync(CreateEvent(BarrierAuditEventType.AgentInvoked));

        await act.Should().NotThrowAsync();
    }

    private static BarrierAuditEvent CreateEvent(string eventType)
        => new(
            EventType: eventType,
            BarrierLevel: "internal",
            AuthoritySource: BarrierAuthoritySource.Cli,
            AgentName: "agent-1",
            CorrelationId: "corr-1",
            SpanId: "span-1",
            OccurredAt: DateTimeOffset.UtcNow,
            Detail: null);
}
