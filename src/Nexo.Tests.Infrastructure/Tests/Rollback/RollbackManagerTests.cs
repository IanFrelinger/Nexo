using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Rollback.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Rollback;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Rollback;

/// <summary>Tests for rollback manager.</summary>
public sealed class RollbackManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public RollbackManagerTests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-rollback");
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
}
