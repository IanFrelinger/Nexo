using FluentAssertions;
using Nexo.Core.Application.Rollback.Models;
using Nexo.Core.Application.Rollback.Ports;
using Xunit;

namespace Nexo.Tests.Contracts;

/// <summary>
/// Contract tests for IRollbackManager implementations.
/// Every implementation must pass these tests to ensure consistent behavior.
/// </summary>
public abstract class RollbackManagerContractTests : IDisposable
{
    protected abstract IRollbackManager CreateInstance();
    protected abstract string GetTempFilePath(string relativePath);

    public virtual void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public async Task PrepareForInherit_BeforeInheritAsync_RollbackAsync_RestoresFile()
    {
        var rollback = CreateInstance();
        var filePath = GetTempFilePath("src/foo.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "original");

        var adaptationId = Guid.NewGuid().ToString("N");
        rollback.PrepareForInherit(adaptationId, new[] { filePath });
        await rollback.BeforeInheritAsync(adaptationId);

        await File.WriteAllTextAsync(filePath, "modified");

        await rollback.RollbackAsync(adaptationId);

        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Be("original");
    }

    [Fact]
    public async Task PreviewRollbackAsync_ReturnsImpact()
    {
        var rollback = CreateInstance();
        var filePath = GetTempFilePath("src/bar.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "original");

        var adaptationId = Guid.NewGuid().ToString("N");
        rollback.PrepareForInherit(adaptationId, new[] { filePath });
        await rollback.BeforeInheritAsync(adaptationId);

        var impact = await rollback.PreviewRollbackAsync(adaptationId);

        impact.TargetAdaptationId.Should().Be(adaptationId);
    }
}
