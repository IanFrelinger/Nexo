using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Adaptation.Generation;

namespace Nexo.Tests.Infrastructure.Certification.Dogfood;

/// <summary>
/// Honest and broken composition specs for damage-resolver → health-applier dogfood.
/// Witness is human-authored separately in <see cref="CompositionDogfoodWitness"/>.
/// </summary>
public static class CompositionDogfoodFixtures
{
    public const string CompositionId = "damage-to-health-pipeline";
    public const string DamageNodeId = "damage";
    public const string HealthNodeId = "health";

    public static CompositionSpec HonestSpec() => new(
        CompositionId,
        [
            new CompositionNode(DamageNodeId, CursorGeneratorModel.DamageResolverIntentId),
            new CompositionNode(HealthNodeId, CursorGeneratorModel.HealthApplierIntentId)
        ],
        HonestEdges(),
        [
            new CompositionPort("baseDamage", "int"),
            new CompositionPort("critMultiplierPercent", "int"),
            new CompositionPort("armor", "int"),
            new CompositionPort("isCrit", "bool"),
            new CompositionPort("currentHealth", "int")
        ],
        [new CompositionPort("newHealth", "int")]);

    /// <summary>
    /// Realistic wiring error: health-applier receives <c>currentHealth</c> on the
    /// <c>finalDamage</c> port instead of <c>damage.finalDamage</c>. Both constituents
    /// remain certified — failure is seam/correctness/mutation, not constituents.
    /// </summary>
    public static CompositionSpec BrokenWiringSpec()
    {
        var edges = new List<CompositionEdge>(HonestEdges());
        edges.RemoveAll(e =>
            string.Equals(e.SourceNodeId, DamageNodeId, StringComparison.Ordinal)
            && string.Equals(e.SourcePort, "finalDamage", StringComparison.Ordinal)
            && string.Equals(e.TargetNodeId, HealthNodeId, StringComparison.Ordinal)
            && string.Equals(e.TargetPort, "finalDamage", StringComparison.Ordinal));
        edges.Add(new CompositionEdge(
            CompositionGraphConstants.InputNodeId,
            "currentHealth",
            HealthNodeId,
            "finalDamage"));

        return HonestSpec() with { Edges = edges };
    }

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
