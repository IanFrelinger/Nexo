using FluentAssertions;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Barriers;
using Ashlar.Runtime.Barriers.Sinks;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Tests for file barrier audit sink lifetime gap coverage.</summary>
public sealed class FileBarrierAuditSinkLifetimeGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var sink = CreateSink();

        var actSink = () => new FileBarrierAuditSinkLifetime(null!, new TestLogger<FileBarrierAuditSinkLifetime>());
        var actLogger = () => new FileBarrierAuditSinkLifetime(sink, null!);

        actSink.Should().Throw<ArgumentNullException>();
        actLogger.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task StartAsync_completes_immediately()
    {
        var lifetime = new FileBarrierAuditSinkLifetime(
            CreateSink(),
            new TestLogger<FileBarrierAuditSinkLifetime>());

        await lifetime.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_disposes_registered_sink()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "ashlar-audit-lifetime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        var sink = new FileBarrierAuditSink(
            new FileBarrierAuditSinkOptions { Directory = testDir, FlushEveryEvent = true },
            new TestLogger<FileBarrierAuditSink>());
        await sink.WriteAsync(new BarrierAuditEvent(
            EventType: BarrierAuditEventType.AgentInvoked,
            BarrierLevel: "internal",
            AuthoritySource: BarrierAuthoritySource.Cli,
            AgentName: "agent",
            CorrelationId: "corr",
            SpanId: "span",
            OccurredAt: DateTimeOffset.UtcNow,
            Detail: "detail"));

        var lifetime = new FileBarrierAuditSinkLifetime(
            sink,
            new TestLogger<FileBarrierAuditSinkLifetime>());

        await lifetime.StopAsync(CancellationToken.None);

        Directory.GetFiles(testDir, "audit-barriers-*.ndjson").Should().NotBeEmpty();
    }

    private static FileBarrierAuditSink CreateSink()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "ashlar-audit-lifetime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        return new FileBarrierAuditSink(
            new FileBarrierAuditSinkOptions { Directory = testDir, FlushEveryEvent = true },
            new TestLogger<FileBarrierAuditSink>());
    }
}
