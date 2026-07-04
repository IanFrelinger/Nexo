namespace Nexo.Core.Domain.Workflows;

/// <summary>Serialization format for <see cref="OutputNode"/> results.</summary>
public enum OutputFormat
{
    /// <summary>JSON object or array.</summary>
    Json,

    /// <summary>XML document.</summary>
    Xml,

    /// <summary>Comma-separated values.</summary>
    Csv,

    /// <summary>Markdown text.</summary>
    Markdown,

    /// <summary>HTML document fragment.</summary>
    Html,

    /// <summary>PDF binary output.</summary>
    Pdf
}
