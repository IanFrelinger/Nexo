using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Infrastructure.Analysis.BrickAnalyzer;
using Nexo.Tests.Infrastructure;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

public sealed class DotNetRegressionTestRunnerTests
{
    [Fact]
    public async Task RunAsync_WithNonExistentPath_ReturnsFailed()
    {
        var runner = new DotNetRegressionTestRunner(null);
        var result = await runner.RunAsync("/nonexistent/path/to/solution.sln");

        result.AllPassed.Should().BeFalse();
        result.Summary.Should().Contain("not found");
    }

    [Fact]
    public async Task RunAsync_WithNexoTestsSolution_ReturnsResult()
    {
        var repoRoot = TestPaths.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Nexo.sln");

        if (!File.Exists(slnPath))
        {
            throw new InvalidOperationException($"Nexo.sln not found at {slnPath}");
        }

        var runner = new DotNetRegressionTestRunner(Microsoft.Extensions.Logging.Abstractions.NullLogger<DotNetRegressionTestRunner>.Instance);
        var result = await runner.RunAsync(slnPath, filter: "FullyQualifiedName~BrickDecomposerTests");

        result.Should().NotBeNull();
        result.PassedCount.Should().BeGreaterThanOrEqualTo(0);
        result.FailedCount.Should().BeGreaterThanOrEqualTo(0);
        result.Summary.Should().NotBeNullOrEmpty();
    }
}
