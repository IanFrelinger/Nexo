using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Certification;

/// <summary>
/// Builds proposer input from a declared target spec and admitted certified bricks.
/// Does not expose witness cases to the proposer.
/// </summary>
public static class CompositionProposerInputBuilder
{
    public static CompositionProposerInput FromTargetAndRegistry(
        CompositionTargetSignature target,
        IBrickRegistry brickRegistry,
        ICertificationRecordStore certificationStore)
    {
        var catalog = new List<CertifiedBrickCatalogEntry>();

        foreach (var brick in brickRegistry.GetAllBricks())
        {
            var record = certificationStore.Get(brick.Id);
            if (record is null || !record.Admitted || !record.Signed)
                continue;

            catalog.Add(new CertifiedBrickCatalogEntry(
                brick.Id,
                brick.Interface.Inputs.Select(i => new WitnessIoField(i.Name, i.Type)).ToList(),
                brick.Interface.Outputs.Select(o => new WitnessIoField(o.Name, o.Type)).ToList(),
                AtomCertificationReference: FormatAtomCertRef(record)));
        }

        return new CompositionProposerInput(target, catalog);
    }

    public static CompositionTargetSignature TargetFromSpec(CompositionSpec spec) =>
        new(spec.CompositionId, spec.Inputs, spec.Outputs);

    private static string FormatAtomCertRef(CertificationRecord record) =>
        $"{record.BrickId}@{record.Timestamp.UtcDateTime:O}";
}
