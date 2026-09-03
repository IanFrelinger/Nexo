using FluentAssertions;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// <see cref="AstMutationCatalog.CatalogVersion"/> is pinned to what the catalog emits, so a
/// catalog change without a version bump fails here — and a bump without a re-pin fails here too.
///
/// <para><b>Why.</b> The version is stamped on every certificate's <c>mutation-gate</c> pass. It is
/// only worth anything if it moves when the catalog moves: a record saying <c>mutationCatalog=2</c>
/// must mean the same set of possible mutants on every certifier that says so. Two pins: the kind
/// list (a renamed or added kind), and the exact mutant ids over a fixture that carries one site of
/// every kind (a scope-rule change — what counts as a lookup key, which statements are removable,
/// which constructor writes are out of scope — moves this list without touching the kind list).</para>
///
/// <para><b>When this fails.</b> If you changed the catalog on purpose: bump
/// <c>AstMutationCatalog.CatalogVersion</c>, then re-pin the expectations below to the new output
/// and say in the constant's history what changed. If you did not change the catalog: the catalog
/// changed under you — find out how before touching either.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class AstMutationCatalogVersionTests
{
    private static readonly IReadOnlyList<string> BrickReferences =
    [
        typeof(DomainBrick).Assembly.Location,
        typeof(BrickInput).Assembly.Location,
    ];

    /// <summary>
    /// One site per kind, all in one member body, every mutant of which compiles (a mutant that does
    /// not is discarded and would not be counted). Line numbers are load-bearing: the ids below are
    /// <c>{kind}-{line}</c> of THIS text.
    /// </summary>
    private const string KindCoverageBrickSource = """
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Hygiene;

public sealed class KindCoverageBrick : Brick
{
    public KindCoverageBrick()
    {
        Id = "kind-coverage";
        Name = "kind-coverage";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "one site per mutation kind";
        Interface = new BrickInterface
        {
            Inputs = [ new BrickInputDefinition("value", "int", "value") ],
            Outputs =
            [
                new BrickOutputDefinition("result", "int", "result"),
                new BrickOutputDefinition("label", "string", "label")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var value = input.Get<int>("value");
        var result = value + 2;
        result -= 1;
        var negative = -value;
        string? label = null;
        label ??= "even";
        if (value == 0 && result < 10)
        {
            label = "zero";
        }
        if (!(negative > 0))
        {
            result++;
        }
        var output = new BrickOutput();
        output.Set("result", result);
        output.Set("label", label);
        return Task.FromResult(output);
    }
}
""";

    [Fact(Timeout = TestTimeouts.Quick)]
    public Task TheCatalogVersion_IsPinnedToTheKindList_AndToWhatTheCatalogEmits()
    {
        AstMutationCatalog.CatalogVersion.Should().Be("2",
            "this pin and the two below move together: a catalog change bumps the version, a bump re-pins the output");

        AstMutationCatalog.Kinds.Should().Equal(
            "flip-binary-op",
            "negate-condition",
            "mutate-int-literal",
            "mutate-string-literal",
            "remove-statement",
            "swap-logical-op",
            "degrade-coalesce-assign",
            "swap-arithmetic-op",
            "swap-arithmetic-assign",
            "shift-relational-boundary",
            "swap-unary-op",
            "remove-logical-not");

        var mutations = AstMutationCatalog.CollectMutations(KindCoverageBrickSource, BrickReferences);
        var ids = mutations.Select(m => m.Id).ToArray();

        ids.Select(KindOf).Distinct().Should().BeEquivalentTo(AstMutationCatalog.Kinds,
            "the fixture has a site for every kind the list names, and the catalog emits nothing the list does not name; ids were [{0}]",
            string.Join(", ", ids));
        ids.Should().Equal(
            [
                "flip-binary-op-38", "flip-binary-op-38#2", "flip-binary-op-42",
                "negate-condition-38",
                "mutate-int-literal-33", "mutate-int-literal-34", "mutate-int-literal-38", "mutate-int-literal-38#2", "mutate-int-literal-42",
                "mutate-string-literal-37", "mutate-string-literal-40", "mutate-string-literal-47", "mutate-string-literal-48",
                "remove-statement-40", "remove-statement-44",
                "swap-logical-op-38",
                "degrade-coalesce-assign-37",
                "swap-arithmetic-op-33",
                "swap-arithmetic-assign-34",
                "shift-relational-boundary-38", "shift-relational-boundary-42",
                "swap-unary-op-35", "swap-unary-op-44",
                "remove-logical-not-42",
            ],
            "the exact mutant set over this fixture is the catalog's fingerprint at version {0}; a scope-rule change moves it (note: the input lookup key on line 32 and the non-compiling removal of line 32 are correctly absent)",
            AstMutationCatalog.CatalogVersion);
        return Task.CompletedTask;
    }

    /// <summary>The kind of an id: everything before the trailing <c>-{line}</c> (and any <c>#n</c>).</summary>
    private static string KindOf(string id) => id[..id.LastIndexOf('-')];
}
