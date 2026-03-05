using FluentAssertions;
using Nexo.Core.Application.SelfImprovement.Models;
using Nexo.Core.Application.SelfImprovement.Ports;
using Nexo.Infrastructure.SelfImprovement;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.SelfImprovement;

/// <summary>
/// P3.4: Holdout pass rate metrics tests.
/// Full holdout validation E2E is covered by nexo improve --self with --holdout-filter in dogfood.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SelfImprovementLoopHoldoutTests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public SelfImprovementLoopHoldoutTests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-holdout");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact]
    public void SelfImprovementReport_HoldoutFields_Populated()
    {
        var report = new SelfImprovementReport(
            DateTimeOffset.UtcNow,
            1, 1, 1, 1, 0,
            new[] { "a1" },
            Array.Empty<string>(),
            0, 0, null,
            HoldoutPassed: 8,
            HoldoutTotal: 10,
            HoldoutPassRate: 0.8);

        report.HoldoutPassed.Should().Be(8);
        report.HoldoutTotal.Should().Be(10);
        report.HoldoutPassRate.Should().Be(0.8);
    }

    [Fact]
    public async Task SelfImprovementLoop_HoldoutPassRate_IsTracked_InMetrics()
    {
        var metricsPath = Path.Combine(Path.GetTempPath(), $"nexo-metrics-{Guid.NewGuid():N}.json");
        var metricsStore = new FileBasedSelfImprovementMetricsStore(metricsPath);

        var report = new SelfImprovementReport(
            DateTimeOffset.UtcNow,
            1, 1, 1, 1, 0,
            new[] { "a1" },
            Array.Empty<string>(),
            0, 0, null,
            HoldoutPassed: 8,
            HoldoutTotal: 10,
            HoldoutPassRate: 0.8);

        await metricsStore.SaveAsync(report);

        var loaded = await metricsStore.GetLastAsync();
        loaded.Should().NotBeNull();
        loaded!.HoldoutPassed.Should().Be(8);
        loaded.HoldoutTotal.Should().Be(10);
        loaded.HoldoutPassRate.Should().Be(0.8);
    }
}
