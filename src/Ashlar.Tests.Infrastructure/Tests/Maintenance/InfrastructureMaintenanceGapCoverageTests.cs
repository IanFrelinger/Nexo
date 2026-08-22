using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ashlar.Core.Application.Maintenance.Models;
using Ashlar.Core.Application.Maintenance.Ports;
using Ashlar.Infrastructure.Maintenance;
using Ashlar.Infrastructure.Maintenance.Ports;
using Ashlar.Infrastructure.Maintenance.Strategies;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Maintenance;

/// <summary>Tests for infrastructure maintenance gap coverage.</summary>
public class InfrastructureMaintenanceGapCoverageTests
{
    [Fact]
    public async Task IncompleteBlobCleanupStrategy_deletes_incomplete_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "blob-cleanup-" + Guid.NewGuid());
        var shaDir = Path.Combine(root, "sha256");
        Directory.CreateDirectory(shaDir);
        var incomplete = Path.Combine(shaDir, "abc.incomplete");
        await File.WriteAllBytesAsync(incomplete, new byte[] { 1, 2, 3 });

        var lifecycle = new Mock<IBlobStorageLifecycle>();
        lifecycle.Setup(l => l.PauseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lifecycle.Setup(l => l.ResumeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var strategy = new IncompleteBlobCleanupStrategy(
            NullLogger<IncompleteBlobCleanupStrategy>.Instance,
            Options.Create(new IncompleteBlobCleanupOptions { BlobStoragePath = root }),
            lifecycle.Object);

        strategy.Id.Should().Be(IncompleteBlobCleanupStrategy.IdConstant);
        var result = await strategy.CleanAsync(new ArtifactCleanupContext(), CancellationToken.None);

        result.PathsDeleted.Should().Contain(incomplete);
        result.BytesReclaimed.Should().Be(3);
        File.Exists(incomplete).Should().BeFalse();

        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task IncompleteBlobCleanupStrategy_skips_when_path_missing()
    {
        var strategy = new IncompleteBlobCleanupStrategy(
            NullLogger<IncompleteBlobCleanupStrategy>.Instance,
            Options.Create(new IncompleteBlobCleanupOptions { BlobStoragePath = "/does/not/exist" }));

        var result = await strategy.CleanAsync(new ArtifactCleanupContext(), CancellationToken.None);
        result.PathsDeleted.Should().BeEmpty();
    }

    [Fact]
    public async Task IncompleteBlobCleanupStrategy_records_failure_when_pause_throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "blob-cleanup-fail-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        var lifecycle = new Mock<IBlobStorageLifecycle>();
        lifecycle.Setup(l => l.PauseAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("pause failed"));
        lifecycle.Setup(l => l.ResumeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var strategy = new IncompleteBlobCleanupStrategy(
            NullLogger<IncompleteBlobCleanupStrategy>.Instance,
            Options.Create(new IncompleteBlobCleanupOptions { BlobStoragePath = root }),
            lifecycle.Object);

        try
        {
            var result = await strategy.CleanAsync(new ArtifactCleanupContext(), CancellationToken.None);
            result.Errors.Should().ContainSingle(e => e == "pause failed");
            lifecycle.Verify(l => l.ResumeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task IncompleteBlobCleanupStrategy_records_resume_failure_after_cleanup_error()
    {
        var root = Path.Combine(Path.GetTempPath(), "blob-cleanup-resume-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        var lifecycle = new Mock<IBlobStorageLifecycle>();
        lifecycle.Setup(l => l.PauseAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("pause failed"));
        lifecycle.Setup(l => l.ResumeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("resume failed"));

        var strategy = new IncompleteBlobCleanupStrategy(
            NullLogger<IncompleteBlobCleanupStrategy>.Instance,
            Options.Create(new IncompleteBlobCleanupOptions { BlobStoragePath = root }),
            lifecycle.Object);

        try
        {
            var result = await strategy.CleanAsync(new ArtifactCleanupContext(), CancellationToken.None);
            result.Errors.Should().Contain("pause failed");
            result.Errors.Should().Contain("Resume failed: resume failed");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
