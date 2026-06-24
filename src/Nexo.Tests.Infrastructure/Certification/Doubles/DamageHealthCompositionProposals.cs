using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Adaptation.Generation;
using Nexo.Infrastructure.Certification.Composition;

namespace Nexo.Tests.Infrastructure.Certification.Doubles;

/// <summary>
/// Deterministic damage→health composition proposals for controlled proposer variants.
/// </summary>
internal static class DamageHealthCompositionProposals
{
    public const string CompositionId = "damage-to-health-pipeline";
    public const string DamageNodeId = "damage";
    public const string HealthNodeId = "health";
    public const string HallucinatedBrickId = "phantom-damage-aggregator";
    public const string UncertifiedBrickId = "standby-healer";

    public static CompositionSpec Correct(CompositionTargetSignature target) => new(
        target.CompositionId,
        [
            new CompositionNode(DamageNodeId, CursorGeneratorModel.DamageResolverIntentId),
            new CompositionNode(HealthNodeId, CursorGeneratorModel.HealthApplierIntentId)
        ],
        HonestEdges(),
        target.Inputs,
        target.Outputs);

    /// <summary>Swaps brick assignments so health-applier runs on the damage node and vice versa.</summary>
    public static CompositionSpec ReorderedWiring(CompositionTargetSignature target) =>
        Correct(target) with
        {
            Nodes =
            [
                new CompositionNode(DamageNodeId, CursorGeneratorModel.HealthApplierIntentId),
                new CompositionNode(HealthNodeId, CursorGeneratorModel.DamageResolverIntentId)
            ]
        };

    public static CompositionSpec DroppedBrick(CompositionTargetSignature target) =>
        new(
            target.CompositionId,
            [new CompositionNode(DamageNodeId, CursorGeneratorModel.DamageResolverIntentId)],
            [
                new CompositionEdge(CompositionGraphConstants.InputNodeId, "baseDamage", DamageNodeId, "baseDamage"),
                new CompositionEdge(CompositionGraphConstants.InputNodeId, "critMultiplierPercent", DamageNodeId, "critMultiplierPercent"),
                new CompositionEdge(CompositionGraphConstants.InputNodeId, "armor", DamageNodeId, "armor"),
                new CompositionEdge(CompositionGraphConstants.InputNodeId, "isCrit", DamageNodeId, "isCrit"),
                new CompositionEdge(DamageNodeId, "finalDamage", CompositionGraphConstants.OutputNodeId, "newHealth")
            ],
            target.Inputs,
            target.Outputs);

    public static CompositionSpec TypeMismatchEdge(CompositionTargetSignature target)
    {
        var edges = new List<CompositionEdge>(HonestEdges())
        {
            new CompositionEdge(DamageNodeId, "finalDamage", HealthNodeId, "currentHealth")
        };
        edges.RemoveAll(e =>
            string.Equals(e.SourceNodeId, CompositionGraphConstants.InputNodeId, StringComparison.Ordinal)
            && string.Equals(e.SourcePort, "currentHealth", StringComparison.Ordinal)
            && string.Equals(e.TargetNodeId, HealthNodeId, StringComparison.Ordinal)
            && string.Equals(e.TargetPort, "currentHealth", StringComparison.Ordinal));

        return Correct(target) with { Edges = edges };
    }

    public static CompositionSpec HallucinatedDependency(CompositionTargetSignature target) =>
        Correct(target) with
        {
            Nodes =
            [
                new CompositionNode(DamageNodeId, CursorGeneratorModel.DamageResolverIntentId),
                new CompositionNode(HealthNodeId, HallucinatedBrickId)
            ]
        };

    public static CompositionSpec UncertifiedConstituent(CompositionTargetSignature target) =>
        Correct(target) with
        {
            Nodes =
            [
                new CompositionNode(DamageNodeId, CursorGeneratorModel.DamageResolverIntentId),
                new CompositionNode(HealthNodeId, UncertifiedBrickId)
            ]
        };

    private static IReadOnlyList<CompositionEdge> HonestEdges() =>
    [
        new CompositionEdge(CompositionGraphConstants.InputNodeId, "baseDamage", DamageNodeId, "baseDamage"),
        new CompositionEdge(CompositionGraphConstants.InputNodeId, "critMultiplierPercent", DamageNodeId, "critMultiplierPercent"),
        new CompositionEdge(CompositionGraphConstants.InputNodeId, "armor", DamageNodeId, "armor"),
        new CompositionEdge(CompositionGraphConstants.InputNodeId, "isCrit", DamageNodeId, "isCrit"),
        new CompositionEdge(CompositionGraphConstants.InputNodeId, "currentHealth", HealthNodeId, "currentHealth"),
        new CompositionEdge(DamageNodeId, "finalDamage", HealthNodeId, "finalDamage"),
        new CompositionEdge(HealthNodeId, "newHealth", CompositionGraphConstants.OutputNodeId, "newHealth")
    ];
}
