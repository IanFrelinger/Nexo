using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.Transforms;

namespace Nexo.Spike.S1.IntentDensity;

/// <summary>
/// Enforced invariant: every wrong-impl transform tag maps to at least one probe class.
/// </summary>
public static class ProbeCatalogMapping
{
    public static IReadOnlyDictionary<TransformTag, IReadOnlyList<string>> TransformToProbeClasses { get; } =
        Build().ToDictionary(k => k.Key, v => (IReadOnlyList<string>)v.Value);

    public static IReadOnlyList<TransformTag> UnmappedTransformTags()
    {
        var mapped = TransformToProbeClasses.Keys.ToHashSet();
        return TransformCatalog.WrongImplTags.Where(tag => !mapped.Contains(tag)).ToList();
    }

    public static IReadOnlyList<TransformTag> ProbeClassesWithoutCatalogTag()
    {
        var catalogTags = TransformCatalog.WrongImplTags.ToHashSet();
        return ProbeCorpus.All
            .Select(p => p.DivergentTransform)
            .Where(tag => !catalogTags.Contains(tag))
            .Distinct()
            .ToList();
    }

    private static IEnumerable<KeyValuePair<TransformTag, List<string>>> Build()
    {
        foreach (var group in ProbeCorpus.All.GroupBy(p => p.DivergentTransform))
            yield return new KeyValuePair<TransformTag, List<string>>(
                group.Key,
                group.Select(p => p.Id).ToList());
    }
}
