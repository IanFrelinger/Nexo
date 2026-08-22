using Ashlar.Core.Domain.Export;
using Ashlar.Core.Domain.Workflows;

namespace Ashlar.Infrastructure.Export;

/// <summary>
/// Exports workflows in various formats and modes.
/// </summary>
public interface IWorkflowExporter
{
    Task<ExportResult> ExportAsync(
        Workflow workflow,
        ExportConfig config,
        CancellationToken ct = default);
}

