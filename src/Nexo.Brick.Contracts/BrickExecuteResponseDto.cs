namespace Nexo.Brick.Contracts;
/// <summary>
/// Wire DTO for POST /api/bricks/{id}/execute response body.
/// </summary>
public class BrickExecuteResponseDto
{
    /// <summary>Wire format version for backward-compatible deserialization.</summary>
    public string WireFormatVersion { get; set; } = Nexo.Brick.Contracts.WireFormatVersion.Current;

    /// <summary>Whether the brick execution completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Human-readable summary of the execution outcome.</summary>
    public string? Summary { get; set; }

    /// <summary>Output key-value; binary values use {"__type":"bytes","base64":"..."}.</summary>
    public IReadOnlyDictionary<string, object>? Output { get; set; }

    /// <summary>Error message when <see cref="Success"/> is false.</summary>
    public string? Error { get; set; }
}
