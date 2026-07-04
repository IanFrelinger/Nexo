namespace Nexo.Core.Domain.Export;

/// <summary>Packaging shape for exported workflow artifacts.</summary>
public enum OutputFormat
{
    /// <summary>Full project structure with project manifest files.</summary>
    Project,

    /// <summary>Individual source files without project scaffolding.</summary>
    Files,

    /// <summary>All generated code in a single file.</summary>
    SingleFile,

    /// <summary>Downloadable archive of generated artifacts.</summary>
    Zip
}
