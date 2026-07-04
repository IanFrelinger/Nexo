using System.Reflection;
using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Tests for file barrier audit sink gap coverage.</summary>
public sealed class FileBarrierAuditSinkGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var logger = new TestLogger<FileBarrierAuditSink>();
        var options = new FileBarrierAuditSinkOptions { Directory = CreateTempDirectory() };

        var actOptions = () => new FileBarrierAuditSink(null!, logger);
        var actLogger = () => new FileBarrierAuditSink(options, null!);

        actOptions.Should().Throw<ArgumentNullException>();
        actLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task DisposeAsync_can_be_called_multiple_times()
    {
        var testDir = CreateTempDirectory();
        var sink = CreateSink(testDir, new TestLogger<FileBarrierAuditSink>());

        await sink.DisposeAsync();
        var act = async () => await sink.DisposeAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WriteAsync_normalizes_blank_directory_and_prefix()
    {
        var testDir = CreateTempDirectory();
        await using var sink = new FileBarrierAuditSink(
            new FileBarrierAuditSinkOptions
            {
                Directory = "   ",
                FilePrefix = "  ",
                FlushEveryEvent = true,
            },
            new TestLogger<FileBarrierAuditSink>());

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(testDir);
            await sink.WriteAsync(new BarrierAuditEvent(
                EventType: BarrierAuditEventType.ContextInitialized,
                BarrierLevel: "public",
                AuthoritySource: BarrierAuthoritySource.Header,
                AgentName: string.Empty,
                CorrelationId: "corr-normalize",
                SpanId: string.Empty,
                OccurredAt: DateTimeOffset.UtcNow,
                Detail: "normalized"));

            await Task.Delay(100);
            Directory.Exists(Path.Combine(testDir, "audit")).Should().BeTrue();
            GetAuditFiles(Path.Combine(testDir, "audit"), "audit-barriers").Should().NotBeEmpty();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public async Task WriteAsync_null_event_is_discarded_and_logged()
    {
        var testDir = CreateTempDirectory();
        var logger = new TestLogger<FileBarrierAuditSink>();
        await using var sink = CreateSink(testDir, logger);

        await sink.WriteAsync(null!);

        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("null barrier audit event", StringComparison.OrdinalIgnoreCase));
        GetAuditFiles(testDir, "audit-barriers").Should().BeEmpty();
    }

    [Fact]
    public async Task BuildUniqueFilePath_returns_new_path_when_timestamp_file_exists()
    {
        var testDir = CreateTempDirectory();
        var buildUnique = typeof(FileBarrierAuditSink).GetMethod(
            "BuildUniqueFilePath",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        await using var sink = CreateSink(testDir, new TestLogger<FileBarrierAuditSink>());
        var firstPath = (string)buildUnique.Invoke(sink, null)!;
        File.WriteAllText(firstPath, "{}");

        string? resolved = null;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var candidate = (string)buildUnique.Invoke(sink, null)!;
            if (!string.Equals(candidate, firstPath, StringComparison.Ordinal))
            {
                resolved = candidate;
                break;
            }
        }

        resolved.Should().NotBeNull();
        resolved!.Should().NotBe(firstPath);
    }

    [Fact]
    public async Task WriteAsync_logs_when_enqueue_throws()
    {
        var testDir = CreateTempDirectory();
        var logger = new TestLogger<FileBarrierAuditSink>();
        var sink = CreateSink(testDir, logger);

        var channelField = typeof(FileBarrierAuditSink).GetField(
            "_channel",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        channelField.SetValue(sink, null);

        await sink.WriteAsync(new BarrierAuditEvent(
            EventType: BarrierAuditEventType.AgentInvoked,
            BarrierLevel: "internal",
            AuthoritySource: BarrierAuthoritySource.Cli,
            AgentName: "agent",
            CorrelationId: "corr",
            SpanId: "span",
            OccurredAt: DateTimeOffset.UtcNow,
            Detail: "detail"));

        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("Failed to enqueue barrier audit event", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildUniqueFilePath_uses_numeric_suffix_when_base_name_exists()
    {
        var testDir = CreateTempDirectory();
        var buildUnique = typeof(FileBarrierAuditSink).GetMethod(
            "BuildUniqueFilePath",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        await using var sink = CreateSink(testDir, new TestLogger<FileBarrierAuditSink>());
        var firstPath = (string)buildUnique.Invoke(sink, null)!;
        var fileName = Path.GetFileName(firstPath);
        var timestamp = fileName
            .Replace("audit-barriers-", string.Empty, StringComparison.Ordinal)
            .Replace(".ndjson", string.Empty, StringComparison.Ordinal);
        File.WriteAllText(firstPath, "{}");
        File.WriteAllText(Path.Combine(testDir, $"audit-barriers-{timestamp}-1.ndjson"), "{}");

        string? suffixed = null;
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var candidate = (string)buildUnique.Invoke(sink, null)!;
            if (candidate.EndsWith("-2.ndjson", StringComparison.Ordinal))
            {
                suffixed = candidate;
                break;
            }

            if (!File.Exists(candidate))
                File.WriteAllText(candidate, "{}");
        }

        suffixed.Should().NotBeNull();
    }

    [Fact]
    public async Task WriteAsync_logs_when_append_fails_for_invalid_directory()
    {
        var testDir = CreateTempDirectory();
        var blockingFile = Path.Combine(testDir, "blocked");
        await File.WriteAllTextAsync(blockingFile, "not-a-directory");
        var logger = new TestLogger<FileBarrierAuditSink>();
        await using var sink = new FileBarrierAuditSink(
            new FileBarrierAuditSinkOptions
            {
                Directory = blockingFile,
                FlushEveryEvent = true,
            },
            logger);

        await sink.WriteAsync(new BarrierAuditEvent(
            EventType: BarrierAuditEventType.ContextInitialized,
            BarrierLevel: "public",
            AuthoritySource: BarrierAuthoritySource.Header,
            AgentName: string.Empty,
            CorrelationId: "corr-append-fail",
            SpanId: string.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            Detail: "detail"));

        await Task.Delay(200);

        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("Failed to append barrier audit event", StringComparison.OrdinalIgnoreCase));
    }

    private static FileBarrierAuditSink CreateSink(string directory, TestLogger<FileBarrierAuditSink> logger)
    {
        return new FileBarrierAuditSink(
            new FileBarrierAuditSinkOptions
            {
                Directory = directory,
                FlushEveryEvent = true,
            },
            logger);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-audit-gap-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string[] GetAuditFiles(string directory, string filePrefix)
        => Directory.Exists(directory)
            ? Directory.GetFiles(directory, $"{filePrefix}-*.ndjson")
            : [];
}
