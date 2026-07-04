namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Progress report during remote model artifact download.
/// </summary>
public sealed record PullProgress
{
    /// <summary>Model identifier being pulled.</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>Bytes downloaded so far.</summary>
    public long BytesDownloaded { get; init; }

    /// <summary>Total bytes expected for the artifact, when known.</summary>
    public long TotalBytes { get; init; }
}
