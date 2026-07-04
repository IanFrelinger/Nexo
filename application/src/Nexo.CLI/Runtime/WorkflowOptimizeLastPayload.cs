using System.Text.Json;

namespace Nexo.CLI.Runtime;

/// <summary>Persisted snapshot of the most recent <c>workflow optimize</c> winner for downstream tooling.</summary>
public sealed record WorkflowOptimizeLastPayload
{
    /// <summary>UTC timestamp when this snapshot was written.</summary>
    public DateTimeOffset WrittenAtUtc { get; init; }

    /// <summary>Identifier of the optimize run that produced this snapshot.</summary>
    public string OptimizeRunId { get; init; } = string.Empty;

    /// <summary>True when the optimize run completed successfully.</summary>
    public bool Ok { get; init; }

    /// <summary>Winning candidate identifier, if any.</summary>
    public string? WinnerCandidateId { get; init; }

    /// <summary>Run identifier associated with the winning candidate.</summary>
    public string? WinnerRunId { get; init; }

    /// <summary>Model profile identifier applied to the winner.</summary>
    public string? ModelProfileId { get; init; }

    /// <summary>Composition identifier applied to the winner.</summary>
    public string? CompositionId { get; init; }

    /// <summary>Original workflow request identifier, if tracked.</summary>
    public string? RequestId { get; init; }

    /// <summary>Ollama model tags referenced by the winning configuration.</summary>
    public IReadOnlyList<string> OllamaModels { get; init; } = Array.Empty<string>();
}
