using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Observation.Models;

namespace Ashlar.Commercial.Fleet.Contracts.Models;

/// <summary>
/// Wire payload for Phase 4 mesh knowledge replication (export/import between Ashlar.API peers).
/// </summary>
public sealed record MeshKnowledgeExportPayload(
    DateTimeOffset ExportedAt,
    IReadOnlyList<AdaptationRecord> Adaptations,
    IReadOnlyList<ObservedPattern> Patterns);
