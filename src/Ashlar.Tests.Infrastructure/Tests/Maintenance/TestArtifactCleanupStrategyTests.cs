using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Maintenance.Models;
using Ashlar.Infrastructure.Maintenance.Strategies;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Maintenance;

/// <summary>Tests for test artifact cleanup strategy.</summary>
public class TestArtifactCleanupStrategyTests : IDisposable
{
    private readonly string _repoRoot;
    private readonly TestArtifactCleanupStrategy _strategy;

    public TestArtifactCleanupStrategyTests()
    {
        _repoRoot = Path.Combine(Path.GetTempPath(), $"ashlar_cleanup_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_repoRoot);
        _strategy = new TestArtifactCleanupStrategy(NullLogger<TestArtifactCleanupStrategy>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            try
            {
                Directory.Delete(_repoRoot, recursive: true);
            }
            catch
            {
                // Best effort
            }
        }
    }

    [Fact]
    public void Id_ReturnsTestArtifacts()
    {
        _strategy.Id.Should().Be("test-artifacts");
    }

    [Fact]
    public async Task CleanAsync_WhenNoArtifactsExist_ReturnsZeroReclaimed()
    {
        var srcDir = Path.Combine(_repoRoot, "src");
        Directory.CreateDirectory(srcDir);
        Directory.CreateDirectory(Path.Combine(srcDir, "SomeProj"));
        Directory.CreateDirectory(Path.Combine(srcDir, "SomeProj", "bin"));

        var context = new ArtifactCleanupContext(RepoRoot: _repoRoot);
        var result = await _strategy.CleanAsync(context);

        result.StrategyId.Should().Be("test-artifacts");
        result.BytesReclaimed.Should().Be(0);
        result.PathsDeleted.Should().BeEmpty();
        result.Errors.Should().BeEmpty();

        Directory.Exists(Path.Combine(srcDir, "SomeProj")).Should().BeTrue();
        Directory.Exists(Path.Combine(srcDir, "SomeProj", "bin")).Should().BeTrue();
    }

    [Fact]
    public async Task CleanAsync_DeletesTestResultsUnderSrc()
    {
        var srcDir = Path.Combine(_repoRoot, "src");
        var testResultsPath = Path.Combine(srcDir, "Ashlar.Tests.Infrastructure", "TestResults");
        Directory.CreateDirectory(testResultsPath);
        var filePath = Path.Combine(testResultsPath, "coverage.xml");
        await File.WriteAllTextAsync(filePath, "<coverage/>");

        var context = new ArtifactCleanupContext(RepoRoot: _repoRoot);
        var result = await _strategy.CleanAsync(context);

        result.PathsDeleted.Should().Contain(testResultsPath);
        result.BytesReclaimed.Should().BeGreaterThan(0);
        result.Errors.Should().BeEmpty();
        Directory.Exists(testResultsPath).Should().BeFalse();
    }

    [Fact]
    public async Task CleanAsync_DeletesRootTestResultsFolder()
    {
        var testResultsRoot = Path.Combine(_repoRoot, "test-results");
        Directory.CreateDirectory(testResultsRoot);
        var filePath = Path.Combine(testResultsRoot, "smoke.trx");
        await File.WriteAllTextAsync(filePath, "<TestRun/>");

        var context = new ArtifactCleanupContext(RepoRoot: _repoRoot);
        var result = await _strategy.CleanAsync(context);

        result.PathsDeleted.Should().Contain(testResultsRoot);
        result.BytesReclaimed.Should().BeGreaterThan(0);
        result.Errors.Should().BeEmpty();
        Directory.Exists(testResultsRoot).Should().BeFalse();
    }

    [Fact]
    public async Task CleanAsync_LeavesNonTargetPathsUntouched()
    {
        var srcDir = Path.Combine(_repoRoot, "src");
        var binPath = Path.Combine(srcDir, "Ashlar.CLI", "bin");
        var objPath = Path.Combine(srcDir, "Ashlar.CLI", "obj");
        Directory.CreateDirectory(binPath);
        Directory.CreateDirectory(objPath);
        await File.WriteAllTextAsync(Path.Combine(binPath, "ashlar.dll"), "mock");

        var context = new ArtifactCleanupContext(RepoRoot: _repoRoot);
        await _strategy.CleanAsync(context);

        Directory.Exists(binPath).Should().BeTrue();
        Directory.Exists(objPath).Should().BeTrue();
    }

    [Fact]
    public async Task CleanAsync_WithNullRepoRoot_UsesCurrentDirectory()
    {
        var context = new ArtifactCleanupContext(RepoRoot: null);
        var result = await _strategy.CleanAsync(context);

        result.StrategyId.Should().Be("test-artifacts");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanAsync_DeletesNestedTestResults()
    {
        var srcDir = Path.Combine(_repoRoot, "src");
        var nestedPath = Path.Combine(srcDir, "A", "B", "Ashlar.Tests.Domain", "TestResults");
        Directory.CreateDirectory(nestedPath);
        await File.WriteAllTextAsync(Path.Combine(nestedPath, "out.txt"), "x");

        var context = new ArtifactCleanupContext(RepoRoot: _repoRoot);
        var result = await _strategy.CleanAsync(context);

        result.PathsDeleted.Should().Contain(nestedPath);
        result.BytesReclaimed.Should().BeGreaterThan(0);
        Directory.Exists(nestedPath).Should().BeFalse();
    }
}
