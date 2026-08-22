namespace Ashlar.Contracts;

/// <summary>Request to run orchestration against a natural-language or structured prompt.</summary>
/// <param name="Request">Primary orchestration input text.</param>
/// <param name="RuntimeSpecJson">Optional JSON runtime spec overrides (temperature, tokens, etc.).</param>
/// <param name="PreferModel">Preferred model id when multiple are available.</param>
/// <param name="Provider">Preferred LLM provider name.</param>
/// <param name="BarrierLevel">Trust barrier level for this invocation.</param>
/// <param name="PreferredRegion">Preferred cloud region for remote execution.</param>
public sealed record OrchestrationRequest(
    string Request,
    string? RuntimeSpecJson = null,
    string? PreferModel = null,
    string? Provider = null,
    string? BarrierLevel = null,
    string? PreferredRegion = null);
