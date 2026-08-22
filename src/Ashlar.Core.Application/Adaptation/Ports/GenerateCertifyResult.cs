using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Result of a single generate→certify→admit attempt (v0: no silent retry loop).
/// </summary>
/// <param name="Manifest">Compiled brick manifest when admission succeeded.</param>
/// <param name="Decision">Certification gate decision.</param>
/// <param name="BrickTypeName">Assembly-qualified type name when admitted.</param>
public sealed record GenerateCertifyResult(
    BrickManifest? Manifest,
    CertificationDecision Decision,
    string? BrickTypeName);
