using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;

namespace Ashlar.Commercial.Fleet.Infrastructure;

/// <summary>Mesh knowledge import result operation.</summary>
public sealed record MeshKnowledgeImportResult(
    int AdaptationsApplied,
    int AdaptationsSkipped,
    int PatternsApplied,
    int PatternsSkipped);
