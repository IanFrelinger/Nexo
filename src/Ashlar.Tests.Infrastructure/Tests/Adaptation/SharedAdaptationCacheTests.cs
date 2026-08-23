using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Paths;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Adaptation;

/// <summary>
/// P2.3: Shared adaptation cache tests.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Adaptation")]
public sealed class SharedAdaptationCacheTests : TempDirTestBase
{
    private readonly string _sharedPath;
    private readonly string _storePath;

    public SharedAdaptationCacheTests() : base("ashlar-shared-adapt")
    {
        _sharedPath = Path.Combine(TempDir, "shared");
        _storePath = Path.Combine(TempDir, "adapt.db");
    }

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
            FilePath = "src/Ashlar.Infrastructure/Observation/FileSystemEventSource.cs",
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
                ["src/Ashlar.Infrastructure/Observation/FileSystemEventSource.cs"] = System.Text.Encoding.UTF8.GetBytes("// bad"),
            },
            BroadcastAt = record.Timestamp,
        };

        var result = await sync.ValidateAndAdoptAsync(entry);

        result.Should().BeFalse("immutable core path must be rejected");
    }

    [Fact(Timeout = 15000)]
    public async Task MeshSync_BroadcastsPromotion_WithSourcePeerId_PulledWithAttribution()
    {
        var mockRegression = new StubRegressionTestRunner(allPassed: true);
        var options = new SharedAdaptationOptions { SourcePeerId = "peer-abc-123" };
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(_storePath)
            .AddSharedAdaptationCache(_sharedPath, mockRegression, options)
            .BuildServiceProvider();

        var broadcaster = services.GetRequiredService<ISharedAdaptationBroadcaster>();
        var sync = services.GetRequiredService<ISharedAdaptationSync>();

        var entry = new SharedAdaptationEntry
        {
            Id = "mesh-broadcast-test",
            Record = new AdaptationRecord
            {
                Id = "mesh-broadcast-test",
                Timestamp = DateTimeOffset.UtcNow,
                BrickId = "test.brick",
                FailureType = "EmptyCatch",
                FixApplied = AdaptationFixType.Source,
                FilePath = "tests/bar.cs",
                RegressionPassed = true,
                Promoted = true,
                Message = "Mesh broadcast",
            },
            Files = new Dictionary<string, byte[]> { ["tests/bar.cs"] = System.Text.Encoding.UTF8.GetBytes("// mesh") },
            BroadcastAt = DateTimeOffset.UtcNow,
            SourcePeerId = "peer-abc-123",
        };

        await broadcaster.BroadcastAsync(entry);

        var pulled = await sync.PullAsync();
        pulled.Should().HaveCount(1);
        pulled[0].SourcePeerId.Should().Be("peer-abc-123");
    }

    [Fact(Timeout = 15000)]
    public async Task MeshSync_IncomingAdaptation_FromUntrustedPeer_RejectedWhenTrustedListSet()
    {
        var mockRegression = new StubRegressionTestRunner(allPassed: true);
        var options = new SharedAdaptationOptions { TrustedPeerIds = new[] { "trusted-peer-only" } };
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(_storePath)
            .AddSharedAdaptationCache(_sharedPath, mockRegression, options)
            .BuildServiceProvider();

        var sync = services.GetRequiredService<ISharedAdaptationSync>();

        var entry = new SharedAdaptationEntry
        {
            Id = "untrusted-entry",
            Record = new AdaptationRecord
            {
                Id = "untrusted-entry",
                Timestamp = DateTimeOffset.UtcNow,
                BrickId = "test.brick",
                FailureType = "EmptyCatch",
                FixApplied = AdaptationFixType.Source,
                FilePath = "tests/foreign.cs",
                RegressionPassed = true,
                Promoted = true,
                Message = "From untrusted",
            },
            Files = new Dictionary<string, byte[]> { ["tests/foreign.cs"] = System.Text.Encoding.UTF8.GetBytes("// untrusted") },
            BroadcastAt = DateTimeOffset.UtcNow,
            SourcePeerId = "untrusted-peer-xyz",
        };

        var result = await sync.ValidateAndAdoptAsync(entry);

        result.Should().BeFalse("adaptation from untrusted peer must be rejected");
    }

    [Fact(Timeout = 15000)]
    public async Task MeshSync_IncomingAdaptation_FromTrustedPeer_Accepted()
    {
        var mockRegression = new StubRegressionTestRunner(allPassed: true);
        var options = new SharedAdaptationOptions { TrustedPeerIds = new[] { "trusted-peer-1" } };
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(_storePath)
            .AddSharedAdaptationCache(_sharedPath, mockRegression, options)
            .BuildServiceProvider();

        var sync = services.GetRequiredService<ISharedAdaptationSync>();
        var log = services.GetRequiredService<IAdaptationLog>();

        var testFilePath = "tests/MeshSync_TrustedPeer_Acceptance_Temp.cs";
        var entry = new SharedAdaptationEntry
        {
            Id = "trusted-entry",
            Record = new AdaptationRecord
            {
                Id = "trusted-entry",
                Timestamp = DateTimeOffset.UtcNow,
                BrickId = "test.brick",
                FailureType = "EmptyCatch",
                FixApplied = AdaptationFixType.Source,
                FilePath = testFilePath,
                RegressionPassed = true,
                Promoted = true,
                Message = "From trusted",
            },
            Files = new Dictionary<string, byte[]> { [testFilePath] = System.Text.Encoding.UTF8.GetBytes("// trusted") },
            BroadcastAt = DateTimeOffset.UtcNow,
            SourcePeerId = "trusted-peer-1",
        };

        try
        {
            var result = await sync.ValidateAndAdoptAsync(entry);

            result.Should().BeTrue("adaptation from trusted peer should be accepted");
            var entries = await log.QueryAsync();
            var adopted = entries.FirstOrDefault(e => e.Id == "trusted-entry");
            adopted.Should().NotBeNull();
            adopted!.Message.Should().Contain("adapted from instance trusted-peer-1");
        }
        finally
        {
            var repoRoot = RepoPathResolver.FindRepoRoot();
            var fullPath = Path.Combine(repoRoot, testFilePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }

    private sealed class StubRegressionTestRunner : IRegressionTestRunner
    {
        private readonly bool _allPassed;

        public StubRegressionTestRunner(bool allPassed) => _allPassed = allPassed;

        public Task<RegressionTestResult> RunAsync(string projectOrSolutionPath, string? filter = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RegressionTestResult { AllPassed = _allPassed, PassedCount = 1, FailedCount = 0, Summary = "Stub" });
    }
}
