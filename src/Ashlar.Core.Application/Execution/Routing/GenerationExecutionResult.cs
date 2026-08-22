using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Unified result returned by any <see cref="IBrickExecutor"/>, regardless
/// of whether execution was local, peer, or cloud.  <see cref="IsRemote"/>
/// and <see cref="Provider"/> let callers trace provenance for auditing.
/// </summary>
public sealed record GenerationExecutionResult
{
    /// <summary>Raw output bytes from the generation job.</summary>
    public byte[] Payload { get; init; } = Array.Empty<byte>();

    /// <summary>Local filesystem path where output was staged, if applicable.</summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>Execution provider name (local, peer node id, RunPod, etc.).</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>Model identifier used for generation.</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>Whether execution occurred on a remote node rather than locally.</summary>
    public bool IsRemote { get; init; }

    /// <summary>Human-readable summary of the generation outcome.</summary>
    public string Summary { get; init; } = string.Empty;
}
