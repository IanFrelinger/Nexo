using Ashlar.Core.Application.Adaptation;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.Infrastructure.Adaptation.Generation;

/// <summary>
/// Single-shot generate→certify→admit pipeline. Generation success does not imply admission.
/// </summary>
public sealed class GenerateAndCertifyService : IGenerateAndCertifyService
{
    private readonly INewBrickGenerator _generator;
    private readonly ICertifiedBrickAdmission _admission;

    /// <summary>Initializes a new generate and certify service.</summary>
    public GenerateAndCertifyService(
        INewBrickGenerator generator,
        ICertifiedBrickAdmission admission)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _admission = admission ?? throw new ArgumentNullException(nameof(admission));
    }

    /// <summary>Generate certify and admit asynchronously.</summary>
    public async Task<GenerateCertifyResult> GenerateCertifyAndAdmitAsync(
        IntentSpec intent,
        WitnessSpec witness,
        CancellationToken cancellationToken = default)
    {
        var signature = WitnessSignatureBuilder.FromWitness(witness);
        var manifest = await _generator.GenerateFromIntentAsync(intent, signature, cancellationToken)
            .ConfigureAwait(false);

        BuiltGeneratedBrick built;
        try
        {
            built = await GeneratedBrickBuilder.BuildAsync(manifest, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var failRecord = new CertificationRecord
            {
                Status = "FAIL",
                Stage = "S0-S2",
                Admitted = false,
                Signed = false,
                Timestamp = DateTimeOffset.UtcNow,
                BrickId = witness.BrickId,
                Reason = $"Build failed: {ex.Message}",
                Gate = nameof(GenerateAndCertifyService)
            };
            return new GenerateCertifyResult(
                manifest,
                new CertificationDecision { Admitted = false, FailureCheck = "build", Record = failRecord },
                BrickTypeName: null);
        }

        var request = new CertificationRequest
        {
            Brick = built.Brick,
            Witness = witness,
            SourceCode = built.SourceCode,
            ProjectPath = built.ProjectPath,
            CompilationReferences = built.CompilationReferences,
            BrickTypeName = built.BrickTypeName,
            EmittedArtifact = built.EmittedArtifact
        };

        var decision = await _admission.CertifyAndAdmitAsync(request, cancellationToken).ConfigureAwait(false);
        return new GenerateCertifyResult(manifest, decision, built.BrickTypeName);
    }
}
