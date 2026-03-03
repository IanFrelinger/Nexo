using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>
/// P2.3: Shared adaptation cache tests.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Adaptation")]
public sealed class SharedAdaptationCacheTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sharedPath;
    private readonly string _storePath;
    private readonly IDisposable _tempDirCleanup;

    public SharedAdaptationCacheTests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-shared-adapt");
        _sharedPath = Path.Combine(_tempDir, "shared");
        _storePath = Path.Combine(_tempDir, "adapt.db");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task Broadcast_ThenPull_ReturnsEntry()
    {
        var mockRegression = new StubRegressionTestRunner(allPassed: true);
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(_storePath)
            .AddSharedAdaptationCache(_sharedPath, mockRegression)
            .BuildServiceProvider();

        var broadcaster = services.GetRequiredService<ISharedAdaptationBroadcaster>();
        var sync = services.GetRequiredService<ISharedAdaptationSync>();

        var record = new AdaptationRecord
        {
            Id = "test-adapt-001",
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "test.brick",
            FailureType = "EmptyCatch",
            FixApplied = AdaptationFixType.Source,
            FilePath = "tests/foo.cs",
            RegressionPassed = true,
            Promoted = true,
            Message = "Test adaptation",
        };
        var content = System.Text.Encoding.UTF8.GetBytes("// fixed");
        var entry = new SharedAdaptationEntry
        {
            Id = record.Id,
            Record = record,
            Files = new Dictionary<string, byte[]> { ["tests/foo.cs"] = content },
            BroadcastAt = record.Timestamp,
        };

        await broadcaster.BroadcastAsync(entry);

        var pulled = await sync.PullAsync();

        pulled.Should().HaveCount(1);
        pulled[0].Id.Should().Be(entry.Id);
        pulled[0].Record.Id.Should().Be(record.Id);
        pulled[0].Files.Should().ContainKey("tests/foo.cs");
        System.Text.Encoding.UTF8.GetString(pulled[0].Files["tests/foo.cs"]).Should().Be("// fixed");
    }

    [Fact(Timeout = 15000)]
    public async Task ValidateAndAdopt_ImmutableCorePath_ReturnsFalse()
    {
        var mockRegression = new StubRegressionTestRunner(allPassed: true);
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(_storePath)
            .AddSharedAdaptationCache(_sharedPath, mockRegression)
            .BuildServiceProvider();

        var sync = services.GetRequiredService<ISharedAdaptationSync>();

        var record = new AdaptationRecord
        {
            Id = "immutable-test",
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "observation.context",
            FailureType = "EmptyCatch",
            FixApplied = AdaptationFixType.Source,
            FilePath = "src/Nexo.Infrastructure/Observation/FileSystemEventSource.cs",
            RegressionPassed = false,
            Promoted = false,
            Message = "Should reject",
        };
        var entry = new SharedAdaptationEntry
        {
            Id = record.Id,
            Record = record,
            Files = new Dictionary<string, byte[]>
            {
                ["src/Nexo.Infrastructure/Observation/FileSystemEventSource.cs"] = System.Text.Encoding.UTF8.GetBytes("// bad"),
            },
            BroadcastAt = record.Timestamp,
        };

        var result = await sync.ValidateAndAdoptAsync(entry);

        result.Should().BeFalse("immutable core path must be rejected");
    }

    private sealed class StubRegressionTestRunner : IRegressionTestRunner
    {
        private readonly bool _allPassed;

        public StubRegressionTestRunner(bool allPassed) => _allPassed = allPassed;

        public Task<RegressionTestResult> RunAsync(string projectOrSolutionPath, string? filter = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RegressionTestResult { AllPassed = _allPassed, PassedCount = 1, FailedCount = 0, Summary = "Stub" });
    }
}
