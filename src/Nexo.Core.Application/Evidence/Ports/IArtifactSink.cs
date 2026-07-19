using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Evidence;

namespace Nexo.Core.Application.Evidence.Ports;

/// <summary>Uploads a local evidence file to an external store.</summary>
public interface IArtifactSink
{
    /// <summary>Provider id used in <see cref="UploadedArtifact.Provider"/>.</summary>
    string Provider { get; }

    /// <summary>Uploads <paramref name="localPath"/> and returns the remote record.</summary>
    Task<UploadedArtifact> UploadAsync(
        string localPath,
        ArtifactUploadRequest request,
        CancellationToken cancellationToken = default);
}
