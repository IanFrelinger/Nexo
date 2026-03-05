namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Sneakernet transport: export/import shared adaptations via file (USB, etc.).
/// P3.3: nexo mesh export, nexo mesh import.
/// </summary>
public interface ISneakernetTransport
{
    /// <summary>
    /// Exports shared adaptations to a file for physical transfer (.nxpkg format).
    /// </summary>
    /// <param name="outputPath">Path to the export file (e.g. nexo-mesh.nxpkg).</param>
    /// <param name="componentId">Optional component/adaptation ID to export; null = export all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportAsync(string outputPath, string? componentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports shared adaptations from an export file and validates/adopts each.
    /// </summary>
    /// <param name="inputPath">Path to the export file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entries adopted.</returns>
    Task<int> ImportAsync(string inputPath, CancellationToken cancellationToken = default);
}
