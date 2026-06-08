using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Observation.Models;

namespace Nexo.Commercial.Fleet.Contracts.Models;

/// <summary>
/// Wire payload for Phase 4 mesh knowledge replication (export/import between Nexo.API peers).
/// </summary>
public sealed record MeshKnowledgeExportPayload(
    DateTimeOffset ExportedAt,
    IReadOnlyList<AdaptationRecord> Adaptations,
    IReadOnlyList<ObservedPattern> Patterns);
