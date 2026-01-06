using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;

namespace Nexo.Infrastructure.Export;

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

