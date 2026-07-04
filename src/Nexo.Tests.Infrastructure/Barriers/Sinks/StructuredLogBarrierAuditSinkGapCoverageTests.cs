using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Tests for structured log barrier audit sink gap coverage.</summary>
public sealed class StructuredLogBarrierAuditSinkGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var logger = new TestLogger<StructuredLogBarrierAuditSink>();

        var actLogger = () => new StructuredLogBarrierAuditSink(null!, new StructuredLogBarrierAuditSinkOptions());
        var actOptions = () => new StructuredLogBarrierAuditSink(logger, null!);

        actLogger.Should().Throw<ArgumentNullException>();
        actOptions.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task WriteAsync_null_event_is_noop()
    {
        var logger = new TestLogger<StructuredLogBarrierAuditSink>();
        var sink = new StructuredLogBarrierAuditSink(logger, new StructuredLogBarrierAuditSinkOptions());

        await sink.WriteAsync(null!);

        logger.Entries.Should().BeEmpty();
    }

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
    public async Task WriteAsync_uses_default_level_for_known_event_types(string eventType, LogLevel expectedLevel)
    {
        var logger = new TestLogger<StructuredLogBarrierAuditSink>();
        var sink = new StructuredLogBarrierAuditSink(logger, new StructuredLogBarrierAuditSinkOptions());

        await sink.WriteAsync(CreateEvent(eventType));

        logger.Entries.Should().ContainSingle(entry => entry.Level == expectedLevel);
    }

    [Fact]
    public async Task WriteAsync_unknown_event_type_defaults_to_warning()
    {
        var logger = new TestLogger<StructuredLogBarrierAuditSink>();
        var sink = new StructuredLogBarrierAuditSink(logger, new StructuredLogBarrierAuditSinkOptions());

        await sink.WriteAsync(CreateEvent("BARRIER_UNKNOWN_EVENT"));

        logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task WriteAsync_applies_event_level_override()
    {
        var logger = new TestLogger<StructuredLogBarrierAuditSink>();
        var options = new StructuredLogBarrierAuditSinkOptions
        {
            EventLevelOverrides = new Dictionary<string, LogLevel>
            {
                ["BARRIER_CEILING_EXCEEDED"] = LogLevel.Critical,
            },
        };
        var sink = new StructuredLogBarrierAuditSink(logger, options);

        await sink.WriteAsync(CreateEvent("BARRIER_CEILING_EXCEEDED"));

        logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task WriteAsync_when_logger_throws_does_not_propagate()
    {
        var sink = new StructuredLogBarrierAuditSink(
            new ThrowingLogger<StructuredLogBarrierAuditSink>(),
            new StructuredLogBarrierAuditSinkOptions());

        var act = async () => await sink.WriteAsync(CreateEvent("BARRIER_AGENT_INVOKED"));

        await act.Should().NotThrowAsync();
    }

    private static BarrierAuditEvent CreateEvent(string eventType)
        => new(
            eventType,
            "internal",
            BarrierAuthoritySource.Cli,
            AgentName: "agent",
            CorrelationId: "corr",
            SpanId: "span",
            DateTimeOffset.UtcNow,
            Detail: "detail");
}
