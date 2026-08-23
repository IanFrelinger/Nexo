using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Rollback.Ports;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Rollback;
using Ashlar.Tests.Application.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Rollback;

/// <summary>Tests for rollback manager.</summary>
public sealed class RollbackManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public RollbackManagerTests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-rollback");
    }

    /// <summary>Dispose.</summary>
    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact]
    public async Task BeforeInherit_CreatesSnapshot()
    {
        var testFile = Path.Combine(_tempDir, "src", "foo.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(testFile)!);
        await File.WriteAllTextAsync(testFile, "original");

        var snapshotPath = Path.Combine(_tempDir, "snapshots");
        var auditPath = Path.Combine(_tempDir, "audit.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<IAdaptationAuditLog>(sp => new LiteDbAdaptationAuditLog(auditPath))
            .AddSingleton<IDependencyGraph, DependencyGraph>()
            .AddSingleton<ISnapshotStore>(_ => new FileSnapshotStore(snapshotPath))
            .AddSingleton<IRollbackManager, RollbackManager>()
            .BuildServiceProvider();

        var rollbackManager = services.GetRequiredService<IRollbackManager>();
        var adaptationId = Guid.NewGuid().ToString("N");

        rollbackManager.PrepareForInherit(adaptationId, new[] { testFile });
        var snapshotId = await rollbackManager.BeforeInheritAsync(adaptationId);

        snapshotId.Should().NotBeNullOrEmpty();
        Directory.Exists(Path.Combine(snapshotPath, snapshotId)).Should().BeTrue();
    }

    [Fact]
    public async Task Rollback_RestoresComponentToPreviousState()
    {
        var testFile = Path.Combine(_tempDir, "src", "foo.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(testFile)!);
        await File.WriteAllTextAsync(testFile, "original");

        var snapshotPath = Path.Combine(_tempDir, "snapshots");
        var auditPath = Path.Combine(_tempDir, "audit.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<IAdaptationAuditLog>(sp => new LiteDbAdaptationAuditLog(auditPath))
            .AddSingleton<IDependencyGraph, DependencyGraph>()
            .AddSingleton<ISnapshotStore>(_ => new FileSnapshotStore(snapshotPath))
            .AddSingleton<IRollbackManager, RollbackManager>()
            .BuildServiceProvider();

        var rollbackManager = services.GetRequiredService<IRollbackManager>();
        var adaptationId = Guid.NewGuid().ToString("N");

        rollbackManager.PrepareForInherit(adaptationId, new[] { testFile });
        await rollbackManager.BeforeInheritAsync(adaptationId);

        await File.WriteAllTextAsync(testFile, "modified");

        await rollbackManager.RollbackAsync(adaptationId);

        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("original");
    }

    [Fact]
    public async Task Rollback_AlsoRollsBackDependents()
    {
        var file1 = Path.Combine(_tempDir, "src", "a.cs");
        var file2 = Path.Combine(_tempDir, "src", "b.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(file1)!);
        await File.WriteAllTextAsync(file1, "a-original");
        await File.WriteAllTextAsync(file2, "b-original");

        var snapshotPath = Path.Combine(_tempDir, "snapshots");
        var auditPath = Path.Combine(_tempDir, "audit.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<IAdaptationAuditLog>(sp => new LiteDbAdaptationAuditLog(auditPath))
            .AddSingleton<IDependencyGraph, DependencyGraph>()
            .AddSingleton<ISnapshotStore>(_ => new FileSnapshotStore(snapshotPath))
            .AddSingleton<IRollbackManager, RollbackManager>()
            .BuildServiceProvider();

        var graph = services.GetRequiredService<IDependencyGraph>();
        graph.Register("adapt-1", Array.Empty<string>());
        graph.Register("adapt-2", new[] { "adapt-1" });

        var rollbackManager = services.GetRequiredService<IRollbackManager>();
        rollbackManager.PrepareForInherit("adapt-1", new[] { file1, file2 });
        await rollbackManager.BeforeInheritAsync("adapt-1");

        await File.WriteAllTextAsync(file1, "a-modified");
        await File.WriteAllTextAsync(file2, "b-modified");

        await rollbackManager.RollbackAsync("adapt-1");

        (await File.ReadAllTextAsync(file1)).Should().Be("a-original");
        (await File.ReadAllTextAsync(file2)).Should().Be("b-original");
    }

    [Fact]
    public async Task Rollback_IsAtomic()
    {
        var file1 = Path.Combine(_tempDir, "src", "a.cs");
        var file2 = Path.Combine(_tempDir, "src", "b.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(file1)!);
        await File.WriteAllTextAsync(file1, "a-original");
        await File.WriteAllTextAsync(file2, "b-original");

        var snapshotPath = Path.Combine(_tempDir, "snapshots");
        var auditPath = Path.Combine(_tempDir, "audit.db");
        var store = new FileSnapshotStore(snapshotPath);
        var snapshotId = await store.TakeSnapshotAsync("test", new[] { file1, file2 });

        await File.WriteAllTextAsync(file1, "a-modified");
        await File.WriteAllTextAsync(file2, "b-modified");

        await store.RestoreSnapshotAsync(snapshotId);

        (await File.ReadAllTextAsync(file1)).Should().Be("a-original");
        (await File.ReadAllTextAsync(file2)).Should().Be("b-original");
    }

    [Fact]
    public async Task Rollback_ByAdaptationId_SucceedsFromAColdProcess()
    {
        // The demo landmine (roadmap audit SHT 08.3): the CLI builds a fresh service
        // provider per invocation, so a RollbackManager that resolves adaptation -> snapshot
        // only from its in-process dictionary can NEVER roll back from a cold start, even
        // though the snapshot sits on disk under the before-inherit label. This test builds
        // a SECOND manager over the same store — the cold CLI, simulated — and requires the
        // rollback to succeed.
        var testFile = Path.Combine(_tempDir, "src", "cold.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(testFile)!);
        await File.WriteAllTextAsync(testFile, "original");

        var snapshotPath = Path.Combine(_tempDir, "snapshots");
        var auditPath = Path.Combine(_tempDir, "audit.db");
        IServiceProvider BuildProcess() => new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<IAdaptationAuditLog>(sp => new LiteDbAdaptationAuditLog(auditPath))
            .AddSingleton<IDependencyGraph, DependencyGraph>()
            .AddSingleton<ISnapshotStore>(_ => new FileSnapshotStore(snapshotPath))
            .AddSingleton<IRollbackManager, RollbackManager>()
            .BuildServiceProvider();

        var adaptationId = Guid.NewGuid().ToString("N");

        // process 1: the adaptation runs, snapshot taken, file mutated
        var warm = BuildProcess().GetRequiredService<IRollbackManager>();
        warm.PrepareForInherit(adaptationId, new[] { testFile });
        await warm.BeforeInheritAsync(adaptationId);
        await File.WriteAllTextAsync(testFile, "mutated by the adaptation");

        // process 2: a cold CLI invocation — brand-new manager, empty dictionary
        var cold = BuildProcess().GetRequiredService<IRollbackManager>();
        await cold.RollbackAsync(adaptationId);

        (await File.ReadAllTextAsync(testFile)).Should().Be(
            "original",
            "the snapshot was on disk the whole time; a cold process must find it by label");
    }

    [Fact]
    public async Task Rollback_ByUnknownAdaptationId_StillFailsClosed()
    {
        var snapshotPath = Path.Combine(_tempDir, "snapshots");
        var auditPath = Path.Combine(_tempDir, "audit.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<IAdaptationAuditLog>(sp => new LiteDbAdaptationAuditLog(auditPath))
            .AddSingleton<IDependencyGraph, DependencyGraph>()
            .AddSingleton<ISnapshotStore>(_ => new FileSnapshotStore(snapshotPath))
            .AddSingleton<IRollbackManager, RollbackManager>()
            .BuildServiceProvider();

        var manager = services.GetRequiredService<IRollbackManager>();

        // The disk fallback must not turn "nothing to restore" into a silent no-op.
        var act = () => manager.RollbackAsync("never-existed");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No snapshot found*never-existed*");
    }
}
