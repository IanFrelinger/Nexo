using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.Tests.Infrastructure.Certification.Doubles;

/// <summary>
/// <strong>TEST DOUBLE</strong> — deterministic stand-in for an untrusted agent composition proposer.
/// Emits caller-specified wiring variants; never sees witness cases.
/// </summary>
internal sealed class ControlledCompositionProposer : ICompositionProposer
{
    public const string CorrectVariant = "correct";
    public const string ReorderedWiringVariant = "reordered-wiring";
    public const string DroppedBrickVariant = "dropped-brick";
    public const string TypeMismatchEdgeVariant = "type-mismatch-edge";
    public const string HallucinatedDependencyVariant = "hallucinated-dependency";
    public const string UncertifiedConstituentVariant = "uncertified-constituent";

    /// <summary>correct | reordered-wiring | dropped-brick | type-mismatch-edge | hallucinated-dependency | uncertified-constituent</summary>
    public string Variant { get; set; } = CorrectVariant;

    public Task<ProposedComposition> ProposeAsync(
        CompositionProposerInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var spec = Variant switch
        {
            CorrectVariant => DamageHealthCompositionProposals.Correct(input.Target),
            ReorderedWiringVariant => DamageHealthCompositionProposals.ReorderedWiring(input.Target),
            DroppedBrickVariant => DamageHealthCompositionProposals.DroppedBrick(input.Target),
            TypeMismatchEdgeVariant => DamageHealthCompositionProposals.TypeMismatchEdge(input.Target),
            HallucinatedDependencyVariant => DamageHealthCompositionProposals.HallucinatedDependency(input.Target),
            UncertifiedConstituentVariant => DamageHealthCompositionProposals.UncertifiedConstituent(input.Target),
            _ => throw new NotSupportedException($"Controlled proposer has no variant '{Variant}'.")
        };

        return Task.FromResult(new ProposedComposition(spec, $"controlled-composition:{Variant}"));
    }
}
