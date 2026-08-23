namespace Ashlar.Brick.Contracts;
/// <summary>
/// Wire DTO for POST /api/bricks/{id}/execute request body.
/// </summary>
public class BrickExecuteRequestDto
{
    /// <summary>Wire format version for backward-compatible deserialization.</summary>
    public string WireFormatVersion { get; set; } = Ashlar.Brick.Contracts.WireFormatVersion.Current;

    /// <summary>Target brick id; must match the route segment and catalog entry.</summary>
    public string BrickId { get; set; } = default!;

    /// <summary>"Deterministic" or "Agentic".</summary>
    public string Implementation { get; set; } = "Deterministic";

    /// <summary>Input key-value; binary values use {"__type":"bytes","base64":"..."}.</summary>
    public IReadOnlyDictionary<string, object>? Input { get; set; }

    /// <summary>Optional execution context (agent, behavior, air-gap flags, prior-step variables).</summary>
    public ExecutionContextDto? ExecutionContext { get; set; }
}
