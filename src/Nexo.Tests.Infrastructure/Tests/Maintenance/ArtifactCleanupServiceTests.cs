using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Maintenance.Models;
using Nexo.Core.Application.Maintenance.Ports;
using Nexo.Infrastructure.Maintenance;
using Nexo.Infrastructure.Maintenance.Strategies;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Maintenance;

public class ArtifactCleanupServiceTests
{
    [Fact]
    public void GetAvailableStrategyIds_ReturnsRegisteredStrategies()
    {
        var strategies = new IArtifactCleanupStrategy[]
        {
            new TestArtifactCleanupStrategy(NullLogger<TestArtifactCleanupStrategy>.Instance)
        };
        var service = new ArtifactCleanupService(strategies, NullLogger<ArtifactCleanupService>.Instance);

        var ids = service.GetAvailableStrategyIds();

        ids.Should().Contain("test-artifacts");
    }

    [Fact]
    public async Task CleanAsync_WithUnknownStrategyId_ReturnsErrorResult()
    {
        var strategies = new IArtifactCleanupStrategy[]
        {
            new TestArtifactCleanupStrategy(NullLogger<TestArtifactCleanupStrategy>.Instance)
        };
        var service = new ArtifactCleanupService(strategies, NullLogger<ArtifactCleanupService>.Instance);

        var result = await service.CleanAsync("unknown-strategy", new ArtifactCleanupContext());

        result.StrategyId.Should().Be("unknown-strategy");
        result.BytesReclaimed.Should().Be(0);
        result.PathsDeleted.Should().BeEmpty();
        result.Errors.Should().ContainSingle(e => e.Contains("not found"));
    }

    [Fact]
    public async Task CleanAsync_WithNullStrategyId_RunsAllStrategies()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), $"nexo_service_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(repoRoot);
        try
        {
            var srcDir = Path.Combine(repoRoot, "src");
            Directory.CreateDirectory(srcDir);
            var testResultsPath = Path.Combine(srcDir, "Proj", "TestResults");
            Directory.CreateDirectory(testResultsPath);
            await File.WriteAllTextAsync(Path.Combine(testResultsPath, "x.txt"), "data");

            var strategies = new IArtifactCleanupStrategy[]
            {
                new TestArtifactCleanupStrategy(NullLogger<TestArtifactCleanupStrategy>.Instance)
            };
            var service = new ArtifactCleanupService(strategies, NullLogger<ArtifactCleanupService>.Instance);

            var result = await service.CleanAsync(strategyId: null, new ArtifactCleanupContext(RepoRoot: repoRoot));

            result.StrategyId.Should().Be("all");
            result.PathsDeleted.Should().NotBeEmpty();
            result.BytesReclaimed.Should().BeGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    [Fact]
    public async Task CleanAsync_WithSpecificStrategy_RunsOnlyThatStrategy()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), $"nexo_service_specific_{Guid.NewGuid():N}");
        Directory.CreateDirectory(repoRoot);
        try
        {
            var testResultsRoot = Path.Combine(repoRoot, "test-results");
            Directory.CreateDirectory(testResultsRoot);
            await File.WriteAllTextAsync(Path.Combine(testResultsRoot, "a.trx"), "trx");

            var strategies = new IArtifactCleanupStrategy[]
            {
                new TestArtifactCleanupStrategy(NullLogger<TestArtifactCleanupStrategy>.Instance)
            };
            var service = new ArtifactCleanupService(strategies, NullLogger<ArtifactCleanupService>.Instance);

            var result = await service.CleanAsync("test-artifacts", new ArtifactCleanupContext(RepoRoot: repoRoot));

            result.StrategyId.Should().Be("test-artifacts");
            result.PathsDeleted.Should().Contain(testResultsRoot);
            result.Errors.Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    [Fact]
    public async Task CleanAsync_StrategyIdCaseInsensitive()
    {
        var strategies = new IArtifactCleanupStrategy[]
        {
            new TestArtifactCleanupStrategy(NullLogger<TestArtifactCleanupStrategy>.Instance)
        };
        var service = new ArtifactCleanupService(strategies, NullLogger<ArtifactCleanupService>.Instance);

        var result = await service.CleanAsync("TEST-ARTIFACTS", new ArtifactCleanupContext(RepoRoot: Path.GetTempPath()));

        result.StrategyId.Should().Be("TEST-ARTIFACTS");
        result.Errors.Should().BeEmpty();
    }
}
