using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Payload sent to a RunPod serverless endpoint (or relayed to a peer).
/// <see cref="GenerationParameters"/> carries provider-specific knobs (top-p,
/// repetition penalty, etc.) that are passed through opaquely.
/// <see cref="IsPriority"/> bumps the job to the front of the RunPod queue
/// when the endpoint supports priority scheduling.
/// </summary>
public sealed record RunPodJobPayload
{
    /// <summary>Target model identifier for generation.</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>User or task prompt sent to the model.</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>Provider-specific generation parameters passed through opaquely.</summary>
    public IReadOnlyDictionary<string, object> GenerationParameters { get; init; } = new Dictionary<string, object>();

    /// <summary>MIME-like output format hint (e.g. text/plain).</summary>
    public string OutputFormat { get; init; } = "text/plain";

    /// <summary>When true, requests priority scheduling on supported endpoints.</summary>
    public bool IsPriority { get; init; }
}
