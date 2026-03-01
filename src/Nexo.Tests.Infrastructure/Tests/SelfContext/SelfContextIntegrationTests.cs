using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Infrastructure.SelfContext;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.SelfContext;

/// <summary>
/// Integration tests for SelfContextAssembler with seeded IAdaptationLog, IExecutionTracer, IPatternStore.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SelfContextIntegrationTests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public SelfContextIntegrationTests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-selfcontext");
    }

    public void Dispose()
    {
        _tempDirCleanup.Dispose();
    }

    [Fact]
    public async Task SelfContextAssembler_QueriesLogsAndTracer_ReturnsSummary()
    {
        var adaptationLog = new InMemoryAdaptationLog();
        var executionTracer = new InMemoryExecutionTracer();
        var patternStore = new InMemoryPatternStore();

        await adaptationLog.LogAsync(new AdaptationRecord
        {
            Id = "a1",
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "observation.context",
            FailureType = "EmptyCatch",
            FixApplied = AdaptationFixType.Source,
            FilePath = "/foo/bar.cs",
            RegressionPassed = true,
            Promoted = true,
        });

        await executionTracer.TraceAsync("observe.start", new Dictionary<string, object> { ["path"] = "/repo" }, "/repo", null);
        await executionTracer.TraceAsync("observe.end", new Dictionary<string, object> { ["eventCount"] = 42 }, "/repo", "completed");

        await patternStore.AddAsync(new ObservedPattern
        {
            PatternId = "p1",
            EventType = "repeated-edits",
            Frequency = 3,
            FirstSeen = DateTimeOffset.UtcNow.AddMinutes(-5),
            LastSeen = DateTimeOffset.UtcNow,
        });

        var assembler = new SelfContextAssembler(adaptationLog, executionTracer, patternStore);
        var result = await assembler.AssembleAsync(TimeSpan.FromHours(1));

        Assert.NotNull(result.Summary);
        Assert.Contains("Recent Activity Summary", result.Summary);
        Assert.Contains("Adaptations (1)", result.Summary);
        Assert.Contains("Executions (2)", result.Summary);
        Assert.Contains("Patterns (1)", result.Summary);
        Assert.Single(result.RecentAdaptations);
        Assert.Equal(2, result.RecentExecutions.Count);
        Assert.Single(result.RecentPatterns);
    }

    private sealed class InMemoryAdaptationLog : IAdaptationLog
    {
        private readonly ConcurrentBag<AdaptationRecord> _records = new();

        public Task LogAsync(AdaptationRecord record, CancellationToken cancellationToken = default)
        {
            _records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AdaptationRecord>> QueryAsync(DateTimeOffset? since, DateTimeOffset? until, string? brickId, CancellationToken cancellationToken = default)
        {
            var list = _records.Where(r =>
                (!since.HasValue || r.Timestamp >= since.Value) &&
                (!until.HasValue || r.Timestamp <= until.Value) &&
                (brickId == null || r.BrickId == brickId)).ToList();
            return Task.FromResult<IReadOnlyList<AdaptationRecord>>(list);
        }
    }

    private sealed class InMemoryExecutionTracer : IExecutionTracer
    {
        private readonly ConcurrentBag<ExecutionTraceEntry> _entries = new();

        public Task TraceAsync(string operation, IReadOnlyDictionary<string, object>? context, string? path, string? outcome, CancellationToken cancellationToken = default)
        {
            _entries.Add(new ExecutionTraceEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = DateTimeOffset.UtcNow,
                Operation = operation,
                Path = path,
                Outcome = outcome,
                Context = context,
            });
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExecutionTraceEntry>> QueryAsync(DateTimeOffset? since, DateTimeOffset? until, CancellationToken cancellationToken = default)
        {
            var list = _entries.Where(e =>
                (!since.HasValue || e.Timestamp >= since.Value) &&
                (!until.HasValue || e.Timestamp <= until.Value)).OrderBy(e => e.Timestamp).ToList();
            return Task.FromResult<IReadOnlyList<ExecutionTraceEntry>>(list);
        }
    }

    private sealed class InMemoryPatternStore : IPatternStore
    {
        private readonly ConcurrentBag<ObservedPattern> _patterns = new();

        public Task AddAsync(ObservedPattern pattern, CancellationToken cancellationToken = default)
        {
            _patterns.Add(pattern);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ObservedPattern>> QueryAsync(PatternStoreQueryParams query, CancellationToken cancellationToken = default)
        {
            var list = _patterns.Where(p =>
                (!query.Since.HasValue || p.LastSeen >= query.Since.Value) &&
                (!query.Until.HasValue || p.FirstSeen <= query.Until.Value)).ToList();
            return Task.FromResult<IReadOnlyList<ObservedPattern>>(list.Take(query.MaxCount).ToList());
        }

        public Task PersistAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
