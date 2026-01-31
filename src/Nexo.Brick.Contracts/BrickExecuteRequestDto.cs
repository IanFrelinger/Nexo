namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for POST /api/bricks/{id}/execute request body.
/// </summary>
public class BrickExecuteRequestDto
{
    public string WireFormatVersion { get; set; } = "2025-01";
    public string BrickId { get; set; } = default!;
    /// <summary>"Deterministic" or "Agentic".</summary>
    public string Implementation { get; set; } = "Deterministic";
    /// <summary>Input key-value; binary values use {"__type":"bytes","base64":"..."}.</summary>
    public IReadOnlyDictionary<string, object>? Input { get; set; }
    public ExecutionContextDto? ExecutionContext { get; set; }
}
