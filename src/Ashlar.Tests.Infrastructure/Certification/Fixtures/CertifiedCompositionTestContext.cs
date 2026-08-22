using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Certification.Composition;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;

namespace Ashlar.Tests.Infrastructure.Certification.Fixtures;

/// <summary>Certified composition test context.</summary>
public sealed class CertifiedCompositionTestContext
{
    public CertifiedCompositionTestContext(
        InMemoryCertificationRecordStore brickStore,
        CertificationRecordSigner brickSigner,
        CertifiedBrickRegistry brickRegistry,
        CertifiedBrickAdmission brickAdmission,
        InMemoryCompositionCertificationRecordStore compositionStore,
        CompositionCertificationRecordSigner compositionSigner,
        CertifiedCompositionRegistry compositionRegistry,
        CompositionCertificationGate compositionGate,
        CertifiedCompositionAdmission compositionAdmission)
    {
        BrickStore = brickStore;
        BrickSigner = brickSigner;
        BrickRegistry = brickRegistry;
        BrickAdmission = brickAdmission;
        CompositionStore = compositionStore;
        CompositionSigner = compositionSigner;
        CompositionRegistry = compositionRegistry;
        CompositionGate = compositionGate;
        CompositionAdmission = compositionAdmission;
    }

    /// <summary>DomainBrick store.</summary>
    public InMemoryCertificationRecordStore BrickStore { get; }
    /// <summary>DomainBrick signer.</summary>
    public CertificationRecordSigner BrickSigner { get; }
    /// <summary>DomainBrick registry.</summary>
    public CertifiedBrickRegistry BrickRegistry { get; }
    /// <summary>DomainBrick admission.</summary>
    public CertifiedBrickAdmission BrickAdmission { get; }
    /// <summary>Composition store.</summary>
    public InMemoryCompositionCertificationRecordStore CompositionStore { get; }
    /// <summary>Composition signer.</summary>
    public CompositionCertificationRecordSigner CompositionSigner { get; }
    /// <summary>Composition registry.</summary>
    public CertifiedCompositionRegistry CompositionRegistry { get; }
    /// <summary>Composition gate.</summary>
    public CompositionCertificationGate CompositionGate { get; }
    /// <summary>Composition admission.</summary>
    public CertifiedCompositionAdmission CompositionAdmission { get; }
}
