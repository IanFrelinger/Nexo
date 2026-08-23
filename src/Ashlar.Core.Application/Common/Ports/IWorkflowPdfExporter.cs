namespace Ashlar.Core.Application.Common.Ports;

/// <summary>
/// Exports workflow output content (e.g. markdown or HTML) to PDF bytes.
/// Implementations live in Infrastructure (e.g. QuestPDF).
/// </summary>
public interface IWorkflowPdfExporter
{
    /// <summary>
    /// Converts the given content (markdown or HTML) to PDF bytes.
    /// </summary>
    /// <param name="content">Text content to render (typically markdown or HTML).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PDF file bytes.</returns>
    Task<byte[]> ExportToPdfAsync(string content, CancellationToken ct = default);
}
