namespace Ashlar.Core.Domain.Export;

/// <summary>
/// Output configuration for export.
/// </summary>
public class OutputConfig
{
    /// <summary>
    /// Output format.
    /// </summary>
    public OutputFormat Format { get; init; } = OutputFormat.Project;
    
    /// <summary>
    /// Namespace for generated code.
    /// </summary>
    public string Namespace { get; init; } = "Generated";
    
    /// <summary>
    /// Include XML documentation.
    /// </summary>
    public bool IncludeDocumentation { get; init; } = true;
    
    /// <summary>
    /// Include unit tests.
    /// </summary>
    public bool IncludeTests { get; init; } = false;
}
