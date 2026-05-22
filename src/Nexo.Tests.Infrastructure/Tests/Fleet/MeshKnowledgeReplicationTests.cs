using FluentAssertions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Observation.Models;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Fleet;
using Nexo.Infrastructure.Observation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Fleet;

public sealed class MeshKnowledgeReplicationTests : IDisposable
{
    private readonly string _dir;

    public MeshKnowledgeReplicationTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "nexo-mesh-knowledge-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }
        catch
        {
            // best effort
        }
    }

    [Fact]
    public async Task Export_then_import_twice_skips_duplicate_adaptations_and_patterns()
    {
        var adaptA = Path.Combine(_dir, "a-adapt.db");
        var patternA = Path.Combine(_dir, "a-patterns.db");
        var adaptB = Path.Combine(_dir, "b-adapt.db");
        var patternB = Path.Combine(_dir, "b-patterns.db");

        var logA = new LiteDbAdaptationLog(adaptA);
        var storeA = new LiteDbPatternStore(patternA);
        var logB = new LiteDbAdaptationLog(adaptB);
        var storeB = new LiteDbPatternStore(patternB);

        await logA.LogAsync(new AdaptationRecord
        {
            Id = "adapt-1",
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "brick-x",
            FailureType = "test",
            FixApplied = AdaptationFixType.Source,
            RegressionPassed = true,
            Promoted = false,
            Message = "seed"
        });

        await storeA.AddAsync(new ObservedPattern
        {
            PatternId = "pat-1",
            EventType = "repeated-edits",
            Frequency = 3,
            FirstSeen = DateTimeOffset.UtcNow.AddMinutes(-10),
            LastSeen = DateTimeOffset.UtcNow,
            ProjectPath = "/tmp"
        });

        var export = new MeshKnowledgeExportService(logA, storeA);
        var payload = await export.ExportAsync(since: null, maxAdaptations: 100, maxPatterns: 100);

        var import = new MeshKnowledgeImportService(logB, storeB);
        var r1 = await import.ImportAsync(payload);
        Assert.Equal(1, r1.AdaptationsApplied);
        Assert.Equal(1, r1.PatternsApplied);

        var r2 = await import.ImportAsync(payload);
        Assert.True(r2.AdaptationsSkipped > 0);
        Assert.True(r2.PatternsSkipped > 0);
    }
}
