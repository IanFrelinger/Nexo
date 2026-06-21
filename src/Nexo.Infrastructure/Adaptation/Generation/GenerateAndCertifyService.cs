using Nexo.Core.Application.Adaptation;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Adaptation.Generation;

/// <summary>
/// Single-shot generate→certify→admit pipeline. Generation success does not imply admission.
/// </summary>
public sealed class GenerateAndCertifyService : IGenerateAndCertifyService
{
    private readonly INewBrickGenerator _generator;
    private readonly ICertifiedBrickAdmission _admission;

    public GenerateAndCertifyService(
        INewBrickGenerator generator,
        ICertifiedBrickAdmission admission)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _admission = admission ?? throw new ArgumentNullException(nameof(admission));
    }

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
            BrickTypeName = built.BrickTypeName
        };

        var decision = await _admission.CertifyAndAdmitAsync(request, cancellationToken).ConfigureAwait(false);
        return new GenerateCertifyResult(manifest, decision, built.BrickTypeName);
    }
}
