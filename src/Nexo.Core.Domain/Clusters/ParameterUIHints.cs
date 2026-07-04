namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// UI hints for parameter rendering.
/// </summary>
public class ParameterUIHints
{
    public string? ControlType { get; init; } // "slider", "dropdown", "text", "color", etc.
    public string? Group { get; init; }
    public int? Order { get; init; }
    public bool Advanced { get; init; } = false;
}
