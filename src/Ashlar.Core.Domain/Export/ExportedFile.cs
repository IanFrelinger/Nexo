namespace Ashlar.Core.Domain.Export;

/// <summary>
/// A file generated during export.
/// </summary>
public class ExportedFile
{
    /// <summary>Relative output path within the export bundle.</summary>
    public string Path { get; init; } = default!;

    /// <summary>Generated file contents.</summary>
    public string Content { get; init; } = default!;

    /// <summary>Language or format label for syntax highlighting.</summary>
    public string Language { get; init; } = default!;
}
