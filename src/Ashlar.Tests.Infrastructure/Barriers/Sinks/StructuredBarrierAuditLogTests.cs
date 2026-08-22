using FluentAssertions;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Barriers;
using Ashlar.Runtime.Barriers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Tests for structured barrier audit log.</summary>
public sealed class StructuredBarrierAuditLogTests
{
    [Fact]
    public async Task RecordAsync_TwoSinksRegistered_BothReceiveEvent()
    {
        var logger = new TestLogger<StructuredBarrierAuditLog>();
        var sink1 = new CapturingSink();
        var sink2 = new CapturingSink();
        var sut = new StructuredBarrierAuditLog(logger, [sink1, sink2]);
        var auditEvent = CreateEvent();

        await sut.RecordAsync(auditEvent, CancellationToken.None);

        sink1.Events.Should().ContainSingle().Which.Should().BeSameAs(auditEvent);
        sink2.Events.Should().ContainSingle().Which.Should().BeSameAs(auditEvent);
    }

    [Fact]
    public async Task RecordAsync_FirstSinkThrows_SecondSinkStillCalled_AndWarningLogged()
    {
        var logger = new TestLogger<StructuredBarrierAuditLog>();
        var sink1 = new ThrowingSink();
        var sink2 = new CapturingSink();
        var sut = new StructuredBarrierAuditLog(logger, [sink1, sink2]);

        await sut.RecordAsync(CreateEvent(), CancellationToken.None);

        sink2.Events.Should().ContainSingle();
        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("Barrier audit sink failure", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Constructor_ZeroSinks_EmitsOneTimeWarningOnly()
    {
        var logger = new TestLogger<StructuredBarrierAuditLog>();
        var sut = new StructuredBarrierAuditLog(logger, []);

        await sut.RecordAsync(CreateEvent(), CancellationToken.None);
        await sut.RecordAsync(CreateEvent(), CancellationToken.None);

        logger.Entries.Count(entry =>
                entry.Level == LogLevel.Warning &&
                entry.Message.Contains("No IBarrierAuditSink registered", StringComparison.Ordinal))
            .Should().Be(1);
    }

    [Fact]
    public async Task RecordAsync_PassesCancellationTokenToEachSink()
    {
        var logger = new TestLogger<StructuredBarrierAuditLog>();
        var sink1 = new CapturingSink();
        var sink2 = new CapturingSink();
        var sut = new StructuredBarrierAuditLog(logger, [sink1, sink2]);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await sut.RecordAsync(CreateEvent(), token);

        sink1.Tokens.Should().ContainSingle().Which.Should().Be(token);
        sink2.Tokens.Should().ContainSingle().Which.Should().Be(token);
    }

    private static BarrierAuditEvent CreateEvent()
        => new(
            BarrierAuditEventType.AgentInvoked,
            "internal",
            BarrierAuthoritySource.Cli,
            "agent-1",
            "corr-1",
            "span-1",
            DateTimeOffset.UtcNow);

    /// <summary>Capturing sink.</summary>
    private sealed class CapturingSink : IBarrierAuditSink
    {
        /// <summary>Events.</summary>
        public List<BarrierAuditEvent> Events { get; } = [];
        /// <summary>Tokens.</summary>
        public List<CancellationToken> Tokens { get; } = [];

        public ValueTask WriteAsync(BarrierAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            Tokens.Add(cancellationToken);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Throwing sink.</summary>
    private sealed class ThrowingSink : IBarrierAuditSink
    {
        public ValueTask WriteAsync(BarrierAuditEvent auditEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Injected sink failure");
    }
}
