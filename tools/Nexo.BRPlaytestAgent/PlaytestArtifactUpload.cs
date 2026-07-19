using Nexo.Core.Application.Evidence.Ports;
using Nexo.Core.Domain.Evidence;
using Nexo.Infrastructure.Evidence;

namespace Nexo.BRPlaytestAgent;

/// <summary>
/// BR playtest glue: uploads session videos through <see cref="IArtifactSink"/>
/// and patches the product report.
/// </summary>
internal static class PlaytestArtifactUpload
{
    public static async Task<IReadOnlyList<UploadedArtifact>> UploadVideosAsync(
        IReadOnlyList<string> videos,
        CancellationToken cancellationToken)
    {
        var sink = GoogleDriveArtifactSink.TryCreateFromEnvironment();
        if (sink is null || videos.Count == 0)
            return Array.Empty<UploadedArtifact>();

        var uploads = new List<UploadedArtifact>(videos.Count);
        foreach (var video in videos)
        {
            uploads.Add(
                await sink.UploadAsync(
                        video,
                        new ArtifactUploadRequest(
                            contentType: "video/mp4",
                            shareAnyoneWithLink: true),
                        cancellationToken)
                    .ConfigureAwait(false));
        }

        return uploads;
    }

    public static Task PatchReportAsync(
        string reportPath,
        IReadOnlyList<string> videos,
        IReadOnlyList<UploadedArtifact> uploads,
        CancellationToken cancellationToken)
        => FileSystemEvidenceBundleWriter.PatchReportUploadsAsync(
            reportPath,
            videos,
            uploads,
            cancellationToken);
}
