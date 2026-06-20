namespace Nexo.Spike.S3.Models;

/// <summary>
/// Describe → persist → Ingest seam envelope.
/// </summary>
public sealed record GenerationHandoff(
    string HandoffId,
    string WorkDirectory,
    string RequestPath,
    GenerationRequest Request,
    bool IsolationEnforced,
    string GeneratorBackend);
