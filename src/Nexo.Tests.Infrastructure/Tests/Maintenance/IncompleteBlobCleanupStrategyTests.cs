using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Maintenance.Models;
using Nexo.Infrastructure.Maintenance;
using Nexo.Infrastructure.Maintenance.Strategies;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Maintenance;

public class IncompleteBlobCleanupStrategyTests : IDisposable
{
    private readonly string _blobPath;
    private readonly IncompleteBlobCleanupStrategy _strategy;

    public IncompleteBlobCleanupStrategyTests()
    {
        _blobPath = Path.Combine(Path.GetTempPath(), $"nexo_blob_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_blobPath);
        var options = Options.Create(new IncompleteBlobCleanupOptions { BlobStoragePath = _blobPath });
        _strategy = new IncompleteBlobCleanupStrategy(
            NullLogger<IncompleteBlobCleanupStrategy>.Instance,
            options,
            lifecycle: null);
    }

    public void Dispose()
    {
        if (Directory.Exists(_blobPath))
        {
            try
            {
                Directory.Delete(_blobPath, recursive: true);
            }
            catch
            {
                // Best effort
            }
        }
    }

    [Fact]
    public void Id_ReturnsIncompleteBlobs()
    {
        _strategy.Id.Should().Be("incomplete-blobs");
    }

    [Fact]
    public async Task CleanAsync_WhenNoIncompleteFiles_ReturnsZeroReclaimed()
    {
        var context = new ArtifactCleanupContext();
        var result = await _strategy.CleanAsync(context);

        result.StrategyId.Should().Be("incomplete-blobs");
        result.BytesReclaimed.Should().Be(0);
        result.PathsDeleted.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanAsync_DeletesIncompleteFilesInBlobPath()
    {
        var sha256Dir = Path.Combine(_blobPath, "sha256");
        Directory.CreateDirectory(sha256Dir);
        var incompletePath = Path.Combine(sha256Dir, "abc123.incomplete");
        await File.WriteAllBytesAsync(incompletePath, new byte[1024]);

        var context = new ArtifactCleanupContext();
        var result = await _strategy.CleanAsync(context);

        result.PathsDeleted.Should().Contain(incompletePath);
        result.BytesReclaimed.Should().Be(1024);
        result.Errors.Should().BeEmpty();
        File.Exists(incompletePath).Should().BeFalse();
    }

    [Fact]
    public async Task CleanAsync_WhenNoSha256Dir_ChecksBlobPath()
    {
        var incompletePath = Path.Combine(_blobPath, "xyz.incomplete");
        await File.WriteAllBytesAsync(incompletePath, new byte[512]);

        var context = new ArtifactCleanupContext();
        var result = await _strategy.CleanAsync(context);

        result.PathsDeleted.Should().Contain(incompletePath);
        result.BytesReclaimed.Should().Be(512);
    }

    [Fact]
    public async Task CleanAsync_WithContextBlobStoragePathOverride_UsesOverride()
    {
        var overridePath = Path.Combine(Path.GetTempPath(), $"nexo_blob_override_{Guid.NewGuid():N}");
        Directory.CreateDirectory(overridePath);
        var incompletePath = Path.Combine(overridePath, "override.incomplete");
        await File.WriteAllBytesAsync(incompletePath, new byte[256]);

        try
        {
            var context = new ArtifactCleanupContext(Options: new Dictionary<string, string>
            {
                ["BlobStoragePath"] = overridePath
            });
            var options = Options.Create(new IncompleteBlobCleanupOptions { BlobStoragePath = _blobPath });
            var strategy = new IncompleteBlobCleanupStrategy(
                NullLogger<IncompleteBlobCleanupStrategy>.Instance,
                options,
                lifecycle: null);

            var result = await strategy.CleanAsync(context);

            result.PathsDeleted.Should().Contain(incompletePath);
            result.BytesReclaimed.Should().Be(256);
        }
        finally
        {
            if (Directory.Exists(overridePath))
                Directory.Delete(overridePath, recursive: true);
        }
    }

    [Fact]
    public async Task CleanAsync_WhenBlobPathDoesNotExist_ReturnsEmptyResult()
    {
        var options = Options.Create(new IncompleteBlobCleanupOptions
        {
            BlobStoragePath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}")
        });
        var strategy = new IncompleteBlobCleanupStrategy(
            NullLogger<IncompleteBlobCleanupStrategy>.Instance,
            options,
            lifecycle: null);

        var context = new ArtifactCleanupContext();
        var result = await strategy.CleanAsync(context);

        result.BytesReclaimed.Should().Be(0);
        result.PathsDeleted.Should().BeEmpty();
    }
}
