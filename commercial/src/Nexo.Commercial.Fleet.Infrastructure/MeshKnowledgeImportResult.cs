using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>Mesh knowledge import result operation.</summary>
public sealed record MeshKnowledgeImportResult(
    int AdaptationsApplied,
    int AdaptationsSkipped,
    int PatternsApplied,
    int PatternsSkipped);
