using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Evidence;

namespace Nexo.Core.Application.Evidence.Ports;

/// <summary>Persists an <see cref="EvidenceBundle"/> and optional upload records.</summary>
public interface IEvidenceBundleWriter
{
    /// <summary>Writes or patches report artifacts for the bundle.</summary>
    Task WriteAsync(
        EvidenceBundle bundle,
        IReadOnlyList<UploadedArtifact>? uploads = null,
        CancellationToken cancellationToken = default);
}
