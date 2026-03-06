using FluentAssertions;
using Nexo.Core.Application.Rollback.Ports;
using Xunit;

namespace Nexo.Tests.Contracts;

/// <summary>
/// Contract tests for ISnapshotStore implementations.
/// Every implementation must pass these tests to ensure consistent behavior.
/// </summary>
public abstract class SnapshotStoreContractTests : IDisposable
{
    protected abstract ISnapshotStore CreateInstance();
    protected abstract string GetTempFilePath(string relativePath);

    public virtual void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public async Task TakeSnapshotAsync_RestoreSnapshotAsync_RestoresIdenticalContent()
    {
        var store = CreateInstance();
        var filePath = GetTempFilePath("src/foo.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "original content");

        var snapshotId = await store.TakeSnapshotAsync("test", new[] { filePath });

        await File.WriteAllTextAsync(filePath, "modified content");

        await store.RestoreSnapshotAsync(snapshotId);

        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Be("original content");
    }

    [Fact]
    public async Task ListSnapshotsAsync_IncludesNewSnapshot()
    {
        var store = CreateInstance();
        var filePath = GetTempFilePath("src/bar.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "x");

        var snapshotId = await store.TakeSnapshotAsync("list-test", new[] { filePath });

        var list = await store.ListSnapshotsAsync();
        list.Should().Contain(s => s.SnapshotId == snapshotId);
    }
}
