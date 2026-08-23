using FluentAssertions;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Infrastructure.Analysis.BrickAnalyzer;
using Ashlar.Tests.Infrastructure;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Analysis;

/// <summary>Tests for dot net regression test runner.</summary>
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
    public async Task RunAsync_WithAshlarTestsSolution_ReturnsResult()
    {
        var repoRoot = TestPaths.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Ashlar.sln");

        if (!File.Exists(slnPath))
        {
            /// <summary>Invalid operation exception.</summary>
            /// <param name="{slnPath}"">{sln path}".</param>
            throw new InvalidOperationException($"Ashlar.sln not found at {slnPath}");
        }

        var runner = new DotNetRegressionTestRunner(Microsoft.Extensions.Logging.Abstractions.NullLogger<DotNetRegressionTestRunner>.Instance);
        var result = await runner.RunAsync(slnPath, filter: "FullyQualifiedName~BrickDecomposerTests");

        result.Should().NotBeNull();
        result.PassedCount.Should().BeGreaterThanOrEqualTo(0);
        result.FailedCount.Should().BeGreaterThanOrEqualTo(0);
        result.Summary.Should().NotBeNullOrEmpty();
    }
}
