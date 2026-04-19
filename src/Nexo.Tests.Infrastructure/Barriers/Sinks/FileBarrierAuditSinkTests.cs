using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

public sealed class FileBarrierAuditSinkTests
{
    /// <summary>Background drain + filesystem visibility can lag on busy Windows hosts and CI.</summary>
    private static readonly TimeSpan IoWait = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task WriteAsync_SingleEvent_WritesValidNdjsonLineWithAllFields()
    {
        var testDir = CreateTempDirectory();
        var logger = new TestLogger<FileBarrierAuditSink>();
        await using var sink = CreateSink(
            testDir,
            logger,
            new FileBarrierAuditSinkOptions
            {
                FlushEveryEvent = true
            });

        await sink.WriteAsync(CreateEvent(1));
        var lines = await WaitForMinimumLinesAsync(testDir, "audit-barriers", 1);

        lines.Should().HaveCount(1);
        AssertLineJson(lines[0], expectedEventType: BarrierAuditEventType.AgentInvoked, expectedCorrelationId: "corr-1");
    }

    [Fact]
    public async Task WriteAsync_MultipleEvents_WritesOneJsonObjectPerLine()
    {
        var testDir = CreateTempDirectory();
        await using var sink = CreateSink(
            testDir,
            new TestLogger<FileBarrierAuditSink>(),
            new FileBarrierAuditSinkOptions { FlushEveryEvent = true });

        for (var i = 0; i < 5; i++)
        {
            await sink.WriteAsync(CreateEvent(i));
        }

        var lines = await WaitForMinimumLinesAsync(testDir, "audit-barriers", 5);
        lines.Should().HaveCount(5);
        foreach (var line in lines)
        {
            Action parse = () => JsonDocument.Parse(line);
            parse.Should().NotThrow();
        }
    }

    [Fact]
    public async Task WriteAsync_RotatesFiles_WhenMaxFileSizeExceeded_AndCapsCount()
    {
        var testDir = CreateTempDirectory();
        var sink = CreateSink(
            testDir,
            new TestLogger<FileBarrierAuditSink>(),
            new FileBarrierAuditSinkOptions
            {
                FlushEveryEvent = true,
                MaxFileSizeBytes = 300,
                MaxRotatedFiles = 4
            });

        for (var i = 0; i < 120; i++)
        {
            await sink.WriteAsync(CreateEvent(i, detail: new string('x', 80)));
        }

        await sink.DisposeAsync();

        await WaitUntilAsync(
            () => GetAuditFiles(testDir, "audit-barriers").Length >= 2,
            IoWait);

        var files = GetAuditFiles(testDir, "audit-barriers");
        files.Length.Should().BeLessOrEqualTo(4);
        files.Length.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task WriteAsync_WhenRotationOverflows_OldestFilesDeleted()
    {
        var testDir = CreateTempDirectory();
        var oldest = Path.Combine(testDir, "audit-barriers-20200101T000000000Z.ndjson");
        var middle = Path.Combine(testDir, "audit-barriers-20200101T000000001Z.ndjson");
        var newest = Path.Combine(testDir, "audit-barriers-20200101T000000002Z.ndjson");
        File.WriteAllText(oldest, "{}");
        File.WriteAllText(middle, "{}");
        File.WriteAllText(newest, "{}");

        var sink = CreateSink(
            testDir,
            new TestLogger<FileBarrierAuditSink>(),
            new FileBarrierAuditSinkOptions
            {
                FlushEveryEvent = true,
                MaxFileSizeBytes = 220,
                MaxRotatedFiles = 2
            });

        for (var i = 0; i < 180; i++)
        {
            await sink.WriteAsync(CreateEvent(i, detail: new string('y', 70)));
        }

        await sink.DisposeAsync();

        await WaitUntilAsync(
            () => GetAuditFiles(testDir, "audit-barriers").Length == 2,
            IoWait);

        File.Exists(oldest).Should().BeFalse();
        GetAuditFiles(testDir, "audit-barriers").Length.Should().Be(2);
    }

    [Fact]
    public async Task WriteAsync_ChannelFull_DropsEventsAndLogsWarning()
    {
        var testDir = CreateTempDirectory();
        var logger = new TestLogger<FileBarrierAuditSink>();
        await using var sink = CreateSink(
            testDir,
            logger,
            new FileBarrierAuditSinkOptions
            {
                ChannelCapacity = 1,
                FlushEveryEvent = true
            });

        var act = () =>
        {
            Parallel.For(0, 2_000, i =>
            {
                sink.WriteAsync(CreateEvent(i, detail: new string('z', 64))).GetAwaiter().GetResult();
            });
        };

        act.Should().NotThrow();

        await WaitUntilAsync(
            () => logger.Entries.Any(entry =>
                entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
                entry.Message.Contains("channel full", StringComparison.OrdinalIgnoreCase)),
            IoWait);
    }

    [Fact]
    public async Task WriteAsync_FlushEveryEvent_ProducesReadableFilePromptly()
    {
        var testDir = CreateTempDirectory();
        await using var sink = CreateSink(
            testDir,
            new TestLogger<FileBarrierAuditSink>(),
            new FileBarrierAuditSinkOptions { FlushEveryEvent = true });

        await sink.WriteAsync(CreateEvent(7));

        await WaitUntilAsync(
            () => GetAllLines(testDir, "audit-barriers").Count >= 1,
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task DisposeAsync_DrainsRemainingChannelEvents()
    {
        var testDir = CreateTempDirectory();
        var sink = CreateSink(
            testDir,
            new TestLogger<FileBarrierAuditSink>(),
            new FileBarrierAuditSinkOptions
            {
                ChannelCapacity = 128,
                FlushEveryEvent = false
            });

        for (var i = 0; i < 50; i++)
        {
            await sink.WriteAsync(CreateEvent(i));
        }

        await sink.DisposeAsync();

        var lines = GetAllLines(testDir, "audit-barriers");
        lines.Should().HaveCount(50);
    }

    [Fact]
    public async Task WriteAsync_CreatesDirectoryWhenMissing()
    {
        var root = CreateTempDirectory();
        var missingDir = Path.Combine(root, "nested", "barriers");
        await using var sink = CreateSink(
            missingDir,
            new TestLogger<FileBarrierAuditSink>(),
            new FileBarrierAuditSinkOptions { FlushEveryEvent = true });

        Directory.Exists(missingDir).Should().BeFalse();
        await sink.WriteAsync(CreateEvent(9));

        await WaitUntilAsync(() => Directory.Exists(missingDir), TimeSpan.FromSeconds(10));
        GetAuditFiles(missingDir, "audit-barriers").Should().NotBeEmpty();
    }

    private static FileBarrierAuditSink CreateSink(
        string directory,
        TestLogger<FileBarrierAuditSink> logger,
        FileBarrierAuditSinkOptions? overrides = null)
    {
        var options = overrides ?? new FileBarrierAuditSinkOptions();
        var configured = new FileBarrierAuditSinkOptions
        {
            Directory = directory,
            FilePrefix = options.FilePrefix,
            MaxFileSizeBytes = options.MaxFileSizeBytes,
            MaxRotatedFiles = options.MaxRotatedFiles,
            FlushIntervalMs = options.FlushIntervalMs,
            FlushEveryEvent = options.FlushEveryEvent,
            ChannelCapacity = options.ChannelCapacity
        };

        return new FileBarrierAuditSink(configured, logger);
    }

    private static BarrierAuditEvent CreateEvent(int index, string? detail = null)
        => new(
            EventType: BarrierAuditEventType.AgentInvoked,
            BarrierLevel: "internal",
            AuthoritySource: BarrierAuthoritySource.Cli,
            AgentName: "CodeGenerationAgent",
            CorrelationId: $"corr-{index}",
            SpanId: $"span-{index}",
            OccurredAt: DateTimeOffset.UtcNow,
            Detail: detail);

    private static void AssertLineJson(string line, string expectedEventType, string expectedCorrelationId)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;
        root.GetProperty("timestamp").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("eventType").GetString().Should().Be(expectedEventType);
        root.GetProperty("barrierLevel").GetString().Should().Be("internal");
        root.GetProperty("authoritySource").GetString().Should().Be(BarrierAuthoritySource.Cli);
        root.GetProperty("agentName").GetString().Should().Be("CodeGenerationAgent");
        root.GetProperty("correlationId").GetString().Should().Be(expectedCorrelationId);
        root.GetProperty("spanId").GetString().Should().StartWith("span-");
        root.TryGetProperty("detail", out _).Should().BeTrue();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-audit-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string[] GetAuditFiles(string directory, string filePrefix)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.GetFiles(directory, $"{filePrefix}-*.ndjson");
    }

    private static IReadOnlyList<string> GetAllLines(string directory, string filePrefix)
    {
        return GetAuditFiles(directory, filePrefix)
            .OrderBy(path => path, StringComparer.Ordinal)
            .SelectMany(ReadLinesWithShareRetry)
            .ToList();
    }

    /// <summary>
    /// The sink may still hold the NDJSON file open on Windows while the drain loop flushes; retry with shared read access.
    /// </summary>
    private static IEnumerable<string> ReadLinesWithShareRetry(string path)
    {
        const int maxAttempts = 80;
        IOException? last = null;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(fs);
                var lines = new List<string>();
                while (reader.ReadLine() is { } line)
                {
                    lines.Add(line);
                }

                return lines;
            }
            catch (IOException ex)
            {
                last = ex;
                if (attempt == maxAttempts - 1)
                    break;
                Thread.Sleep(25);
            }
        }

        throw last ?? new IOException($"Unable to read '{path}'.");
    }

    private static async Task<IReadOnlyList<string>> WaitForMinimumLinesAsync(
        string directory,
        string filePrefix,
        int minimumLineCount)
    {
        IReadOnlyList<string> lines = [];
        await WaitUntilAsync(() =>
        {
            lines = GetAllLines(directory, filePrefix);
            return lines.Count >= minimumLineCount;
        }, IoWait);
        return lines;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }

        condition().Should().BeTrue("condition should complete within timeout {0}", timeout);
    }
}
