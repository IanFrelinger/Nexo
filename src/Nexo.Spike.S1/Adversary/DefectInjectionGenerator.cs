using Nexo.Spike.S1.Transforms;

namespace Nexo.Spike.S1.Adversary;

/// <summary>
/// Offline, seeded adversary that applies the transform catalog to honest S0 fixtures.
/// </summary>
public sealed class DefectInjectionGenerator : IAdversarialGenerator
{
    public IReadOnlyList<AdversarialCandidate> GenerateWrongImplCandidates(int seeds)
    {
        if (seeds < 0)
            throw new ArgumentOutOfRangeException(nameof(seeds));

        var honestImpl = HonestFixtures.Implementation;
        var honestTests = HonestFixtures.Tests;
        var candidates = new List<AdversarialCandidate>(seeds * TransformCatalog.WrongImplTags.Count);

        for (var seed = 0; seed < seeds; seed++)
        {
            foreach (var tag in TransformCatalog.WrongImplTags)
            {
                candidates.Add(new AdversarialCandidate(
                    tag,
                    TransformFamily.WrongImpl,
                    seed,
                    TransformCatalog.ApplyImplTransform(tag, honestImpl),
                    honestTests));
            }
        }

        return candidates;
    }

    public IReadOnlyList<AdversarialCandidate> GenerateWeakTestCandidates(int seeds)
    {
        if (seeds < 0)
            throw new ArgumentOutOfRangeException(nameof(seeds));

        var honestImpl = HonestFixtures.Implementation;
        var honestTests = HonestFixtures.Tests;
        var candidates = new List<AdversarialCandidate>(seeds * TransformCatalog.WeakTestTags.Count);

        for (var seed = 0; seed < seeds; seed++)
        {
            foreach (var tag in TransformCatalog.WeakTestTags)
            {
                candidates.Add(new AdversarialCandidate(
                    tag,
                    TransformFamily.WeakTest,
                    seed,
                    honestImpl,
                    TransformCatalog.ApplyTestTransform(tag, honestTests)));
            }
        }

        return candidates;
    }

    public AdversarialCandidate GenerateHonestBaseline(int seed) =>
        new(
            TransformTag.HonestNoOp,
            TransformFamily.HonestBaseline,
            seed,
            TransformCatalog.ApplyImplTransform(TransformTag.HonestNoOp, HonestFixtures.Implementation),
            TransformCatalog.ApplyTestTransform(TransformTag.HonestNoOp, HonestFixtures.Tests));
}
