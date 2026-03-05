using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Core.Application.SelfImprovement.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Analysis;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>
/// P0.2: Proves the adaptation engine cannot target immutable core components.
/// Rejection is immediate, logged to audit, and throws ImmutableCoreViolationException.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ImmutableCoreAdaptationTests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public ImmutableCoreAdaptationTests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-immutable-adaptation");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact]
    public void ImmutableCore_ComponentList_IsExplicitlyEnumerated_InCode()
    {
        var registry = new ImmutableCoreRegistry();

        registry.CoreComponentIds.Should().NotBeEmpty("immutable core must be explicitly enumerated in code");
        registry.CoreComponentIds.Should().Contain("observation.pipeline");
        registry.CoreComponentIds.Should().Contain("analysis.engine");
        registry.CoreComponentIds.Should().Contain("validation.checker");
        registry.CoreComponentIds.Should().Contain("rollback.manager");
    }

    [Fact]
    public void ImmutableCore_AdaptationEngine_CannotTarget_ObservationPipeline()
    {
        var registry = new ImmutableCoreRegistry();
        var path = "src/Nexo.Infrastructure/Observation/PatternDetector.cs";

        registry.IsInImmutableCore(path).Should().BeTrue();
    }

    [Fact]
    public void ImmutableCore_AdaptationEngine_CannotTarget_RoslynAnalyzer()
    {
        var registry = new ImmutableCoreRegistry();
        var path = "src/Nexo.Infrastructure/Analysis/Rules/SecurityAnalysisRule.cs";

        registry.IsInImmutableCore(path).Should().BeTrue();
    }

    [Fact]
    public void ImmutableCore_AdaptationEngine_CannotTarget_ValidationLayer()
    {
        var registry = new ImmutableCoreRegistry();
        var path = "src/Nexo.Infrastructure/Validation/Parsers/TrxTestResultParser.cs";

        registry.IsInImmutableCore(path).Should().BeTrue();
    }

    [Fact]
    public void ImmutableCore_AdaptationEngine_CannotTarget_TrustComponents()
    {
        var registry = new ImmutableCoreRegistry();
        var path = "src/Nexo.Infrastructure/Trust/AccessBoundary.cs";

        registry.IsInImmutableCore(path).Should().BeTrue();
    }

    [Fact]
    public void ImmutableCore_AdaptationEngine_CannotTarget_RollbackManager()
    {
        var registry = new ImmutableCoreRegistry();
        var path = "src/Nexo.Infrastructure/Rollback/RollbackManager.cs";

        registry.IsInImmutableCore(path).Should().BeTrue();
    }

    [Fact]
    public async Task ImmutableCore_RejectedAttempt_IsWrittenToAuditLog()
    {
        var storePath = Path.Combine(Path.GetTempPath(), $"nexo-immutable-audit-{Guid.NewGuid():N}.db");
        var failures = new List<TestFailureRecord>
        {
            new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = DateTimeOffset.UtcNow,
                TestName = "SomeTest",
                FilePath = "src/Nexo.Infrastructure/Observation/FileSystemEventSource.cs",
                ErrorMessage = "Test failed",
            },
        };

        var auditEntries = new ConcurrentBag<AdaptationAuditEntry>();
        var mockAuditLog = new MockAdaptationAuditLog(auditEntries);

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<ITestFailureStore>(new MockTestFailureStore(failures))
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure(storePath)
            .AddSingleton<IAdaptationAuditLog>(mockAuditLog)
            .AddSelfImprovementLoop(5)
            .BuildServiceProvider();

        var loop = services.GetRequiredService<ISelfImprovementLoop>();

        var act = async () => await loop.RunOnceAsync();

        await act.Should().ThrowAsync<ImmutableCoreViolationException>()
            .WithMessage("*FileSystemEventSource*");

        auditEntries.Should().ContainSingle(e =>
            e.Outcome == "Rejected" &&
            e.FilePath != null &&
            e.FilePath.Contains("Observation", StringComparison.OrdinalIgnoreCase) &&
            e.Message != null &&
            e.Message.Contains("Immutable core", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImmutableCore_RejectionIsImmediate_NotAfterPartialApplication()
    {
        var storePath = Path.Combine(Path.GetTempPath(), $"nexo-immutable-immediate-{Guid.NewGuid():N}.db");
        var failures = new List<TestFailureRecord>
        {
            new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = DateTimeOffset.UtcNow,
                TestName = "SomeTest",
                FilePath = "src/Nexo.Infrastructure/Observation/PatternDetector.cs",
                ErrorMessage = "Test failed",
            },
        };

        var mockAuditLog = new MockAdaptationAuditLog(new ConcurrentBag<AdaptationAuditEntry>());

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<ITestFailureStore>(new MockTestFailureStore(failures))
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure(storePath)
            .AddSingleton<IAdaptationAuditLog>(mockAuditLog)
            .AddSelfImprovementLoop(5)
            .BuildServiceProvider();

        var loop = services.GetRequiredService<ISelfImprovementLoop>();

        var act = async () => await loop.RunOnceAsync();

        await act.Should().ThrowAsync<ImmutableCoreViolationException>();
    }

    private sealed class MockTestFailureStore : ITestFailureStore
    {
        private readonly IReadOnlyList<TestFailureRecord> _records;

        public MockTestFailureStore(IReadOnlyList<TestFailureRecord> records) => _records = records;

        public Task RecordAsync(TestFailureRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<TestFailureRecord>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_records);
    }

    private sealed class MockAdaptationAuditLog : IAdaptationAuditLog
    {
        private readonly ConcurrentBag<AdaptationAuditEntry> _entries;

        public MockAdaptationAuditLog(ConcurrentBag<AdaptationAuditEntry> entries) => _entries = entries;

        public Task LogAsync(AdaptationAuditEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AdaptationAuditEntry>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdaptationAuditEntry>>(_entries.ToList());
    }
}
