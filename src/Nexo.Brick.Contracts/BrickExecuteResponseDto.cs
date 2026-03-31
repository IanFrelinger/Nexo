namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for POST /api/bricks/{id}/execute response body.
/// </summary>
public class BrickExecuteResponseDto
{
    public string WireFormatVersion { get; set; } = Nexo.BrickContracts.WireFormatVersion.Current;
    public bool Success { get; set; }
    public string? Summary { get; set; }
    /// <summary>Output key-value; binary values use {"__type":"bytes","base64":"..."}.</summary>
    public IReadOnlyDictionary<string, object>? Output { get; set; }
    public string? Error { get; set; }
}
