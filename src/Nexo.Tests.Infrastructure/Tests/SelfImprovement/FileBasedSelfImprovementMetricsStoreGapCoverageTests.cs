using FluentAssertions;
using Nexo.Core.Application.Common.Models;
using Moq;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Infrastructure.SelfImprovement;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.SelfImprovement;

/// <summary>Tests for file based self improvement metrics store gap coverage.</summary>
public sealed class FileBasedSelfImprovementMetricsStoreGapCoverageTests
{
    [Fact]
    public async Task GetLastAsync_returns_null_when_file_missing()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-metrics-" + Guid.NewGuid().ToString("N") + ".json");
        var store = new FileBasedSelfImprovementMetricsStore(path);

        (await store.GetLastAsync()).Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_and_GetLastAsync_round_trip_report()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-metrics-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new FileBasedSelfImprovementMetricsStore(path);
            var report = new Nexo.Core.Application.SelfImprovement.Models.SelfImprovementReport(
                RunAt: DateTimeOffset.UtcNow,
                FailuresProcessed: 2,
                FixesGenerated: 1,
                FixesValidated: 1,
                FixesPromoted: 1,
                FixesRejected: 0,
                PromotedAdaptationIds: ["a1"],
                RejectedReasons: []);

            await store.SaveAsync(report);

            var loaded = await store.GetLastAsync();
            loaded.Should().NotBeNull();
            loaded!.FailuresProcessed.Should().Be(2);
            loaded.FixesPromoted.Should().Be(1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
