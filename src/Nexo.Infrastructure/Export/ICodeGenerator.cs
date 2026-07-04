using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;

namespace Nexo.Infrastructure.Export;

/// <summary>
/// Generates code for bricks and workflows.
/// </summary>
public interface ICodeGenerator
{
    Task<string> GenerateDeterministicAsync(DomainBrick brick, ExportTarget target, CancellationToken ct = default);
    Task<string> GenerateDeterministicWithDataAsync(DomainBrick brick, ExportTarget target, CancellationToken ct = default);
    Task<string> GenerateOrchestrationAsync(Workflow workflow, ExportTarget target, CancellationToken ct = default);
    Task<string> GenerateRuntimeBootstrapAsync(Workflow workflow, ExportTarget target, bool includeFallbacks, CancellationToken ct = default);
    Task<ExportedFile> GenerateStaticDataAsync(DomainBrick brick, GeneratedContent content, ExportTarget target, CancellationToken ct = default);
}
