using Nexo.Core.Application.Evidence.Ports;
using Nexo.Core.Domain.Evidence;
using Nexo.Infrastructure.Evidence;
using Xunit;

namespace Nexo.Tests.HostBridge;

public sealed class EvidenceBundleTests
{
    [Fact]
    public async Task LocalCopyArtifactSink_CopiesFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-sink-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source.mp4");
        var destRoot = Path.Combine(root, "out");
        Directory.CreateDirectory(root);
        await File.WriteAllBytesAsync(source, new byte[] { 1, 2, 3, 4 });

        try
        {
            var sink = new LocalCopyArtifactSink(destRoot);
            var uploaded = await sink.UploadAsync(
                source,
                new ArtifactUploadRequest(displayName: "clip.mp4"));

            Assert.Equal("local-copy", uploaded.Provider);
            Assert.Equal(4, uploaded.Bytes);
            Assert.True(File.Exists(Path.Combine(destRoot, "clip.mp4")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task FileSystemEvidenceBundleWriter_WritesBundleAndPatchesReport()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-evidence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var reportPath = Path.Combine(root, "report.json");
        await File.WriteAllTextAsync(reportPath, """{"verdict":"passed","checks":{}}""");

        try
        {
            var bundle = new EvidenceBundle(
                sessionId: "session-1",
                rootPath: root,
                checkpoints: [new EvidenceCheckpoint("initial", """{"ready":true}""")],
                artifacts:
                [
                    new EvidenceArtifact(
                        EvidenceKind.Video,
                        "videos/clip.mp4",
                        12,
                        DateTimeOffset.UtcNow)
                ],
                verdict: "passed");

            var uploads = new[]
            {
                new UploadedArtifact(
                    "local-copy",
                    "id-1",
                    new Uri("file:///tmp/clip.mp4"),
                    12,
                    "/tmp/clip.mp4",
                    "clip.mp4")
            };

            var writer = new FileSystemEvidenceBundleWriter();
            await writer.WriteAsync(bundle, uploads);

            Assert.True(File.Exists(Path.Combine(root, "evidence-bundle.json")));
            Assert.True(File.Exists(Path.Combine(root, "evidence-bundle.md")));

            await FileSystemEvidenceBundleWriter.PatchReportUploadsAsync(
                reportPath,
                ["/tmp/clip.mp4"],
                uploads);

            var patched = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("driveUploads", patched, StringComparison.Ordinal);
            Assert.Contains("clip.mp4", patched, StringComparison.Ordinal);
            Assert.Contains("\"verdict\": \"passed\"", patched, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
