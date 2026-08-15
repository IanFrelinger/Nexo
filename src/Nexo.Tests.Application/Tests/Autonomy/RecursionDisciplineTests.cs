using System.Runtime.Loader;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nexo.Agents.TestKit;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Domain.Bricks.Ports;
using Xunit;

namespace Nexo.Tests.Application.Tests.Autonomy;

/// <summary>
/// Recursion discipline (autonomy spec §4): depth increments and is bound to parent
/// certificate identities (the anti-laundering matrix), the ceiling is lowerable but
/// never raisable by environment (raising is Tier 2), capability envelopes only narrow
/// with depth (R4.4), and self-produced code cannot register as a judge (R4.3).
/// </summary>
public sealed class RecursionDisciplineTests
{
    // --- lineage coherence (R4.1 / §8 depth laundering) ----------------------------------

    [Fact]
    public void HumanAuthored_IsDepthZero_WithNoParents_AndCoherent()
    {
        var lineage = GenerationLineage.HumanAuthored;

        lineage.Depth.Should().Be(0);
        lineage.FindInconsistencies().Should().BeEmpty();
    }

    [Fact]
    public void Child_IncrementsDepth_AndAccumulatesTheParentChain()
    {
        var depth1 = GenerationLineage.Child(GenerationLineage.HumanAuthored, "sig-a");
        var depth2 = GenerationLineage.Child(depth1, "sig-b");

        depth2.Depth.Should().Be(2);
        depth2.ParentCertificateSignatures.Should().Equal("sig-a", "sig-b");
        depth2.FindInconsistencies().Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1, 0, "negative")]
    [InlineData(0, 1, "human-authored context has no self-produced parents")]
    [InlineData(2, 0, "laundering")]
    [InlineData(1, 2, "more parents than generations")]
    public void IncoherentLineages_AreNamed(int depth, int parentCount, string expectedFragment)
    {
        var lineage = new GenerationLineage
        {
            Depth = depth,
            ParentCertificateSignatures = Enumerable.Range(0, parentCount).Select(i => $"sig-{i}").ToArray(),
        };

        lineage.FindInconsistencies().Should().ContainSingle().Which.Should().Contain(expectedFragment);
    }

    // --- ceiling (R4.2) ------------------------------------------------------------------

    [Fact]
    public void Ceiling_DefaultsToTwo_AndEnvironmentMayOnlyLowerIt()
    {
        var previous = Environment.GetEnvironmentVariable(RecursionDiscipline.CeilingEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(RecursionDiscipline.CeilingEnvVar, null);
            RecursionDiscipline.ResolveCeiling().Should().Be(2);

            Environment.SetEnvironmentVariable(RecursionDiscipline.CeilingEnvVar, "1");
            RecursionDiscipline.ResolveCeiling().Should().Be(1, "stricter deployments may lower the ceiling");

            Environment.SetEnvironmentVariable(RecursionDiscipline.CeilingEnvVar, "7");
            RecursionDiscipline.ResolveCeiling().Should().Be(2, "raising the ceiling is Tier 2, not an env var");

            Environment.SetEnvironmentVariable(RecursionDiscipline.CeilingEnvVar, "garbage");
            RecursionDiscipline.ResolveCeiling().Should().Be(2);
        }
        finally
        {
            Environment.SetEnvironmentVariable(RecursionDiscipline.CeilingEnvVar, previous);
        }
    }

    [Fact]
    public void FindViolations_CombinesCoherenceAndCeiling()
    {
        var tooDeep = new GenerationLineage
        {
            Depth = 3,
            ParentCertificateSignatures = ["a", "b", "c"],
        };

        RecursionDiscipline.FindViolations(tooDeep)
            .Should().ContainSingle().Which.Should().Contain("ceiling").And.Contain("Tier 2");
    }

    // --- capability narrowing (R4.4) -----------------------------------------------------

    [Fact]
    public void ChildrenInsideTheParentEnvelope_AreNotWidenings()
    {
        var parent = new TouchSet
        {
            PathPrefixes = ["src/Nexo.Bricks.Weather/"],
            Namespaces = ["Nexo.Bricks.Weather"],
            Capabilities = ["repo.fs.write", "dotnet.build"],
        };
        var child = new TouchSet
        {
            PathPrefixes = ["src/Nexo.Bricks.Weather/Forecast/"],
            Namespaces = ["Nexo.Bricks.Weather.Forecast"],
            Capabilities = ["repo.fs.write"],
        };

        TouchSetNarrowing.FindWidenings(child, parent).Should().BeEmpty();
    }

    [Fact]
    public void EveryWideningDimension_IsNamed()
    {
        var parent = new TouchSet
        {
            PathPrefixes = ["src/Nexo.Bricks.Weather/"],
            Namespaces = ["Nexo.Bricks.Weather"],
            Capabilities = ["repo.fs.write"],
        };
        var child = new TouchSet
        {
            PathPrefixes = ["src/Nexo.Policies/"],
            Namespaces = ["Nexo.Policies"],
            Capabilities = ["repo.fs.write", "network.fetch"],
        };

        var widenings = TouchSetNarrowing.FindWidenings(child, parent);

        widenings.Should().HaveCount(3);
        widenings.Should().Contain(w => w.Contains("src/Nexo.Policies/"));
        widenings.Should().Contain(w => w.Contains("Nexo.Policies"));
        widenings.Should().Contain(w => w.Contains("network.fetch"));
    }

    // --- judge growth (R4.3) -------------------------------------------------------------

    [Fact]
    public void SelfProducedValidator_CannotRegisterAsAJudge()
    {
        var assemblyBytes = CompileValidatorAssembly();

        // Self-produced: the type lives in a COLLECTIBLE context, the shape every
        // hot-swapped or mutation-sandbox artifact has in this process.
        var collectible = new AssemblyLoadContext("judge-growth-test", isCollectible: true);
        try
        {
            using var stream = new MemoryStream(assemblyBytes);
            var type = collectible.LoadFromStream(stream).GetType("SelfProducedValidator")!;
            var judge = Activator.CreateInstance(type)!;

            var act = () => new AgentProfileRegistry().Register(new AgentProfile
            {
                TargetId = "judge-growth",
                Drafter = FakeArtifactDrafter.OfContent("x"),
                Validators = new[] { judge },
            });

            act.Should().Throw<ArgumentException>().WithMessage("*R4.3*",
                "the loop may not grow its own judges without human admission");
        }
        finally
        {
            collectible.Unload();
        }

        // Control: byte-identical code loaded NON-collectibly (an authored deployment)
        // registers fine — the refusal keys on self-production, not on how it compiled.
        var authoredType = System.Reflection.Assembly.Load(assemblyBytes).GetType("SelfProducedValidator")!;
        var authored = Activator.CreateInstance(authoredType)!;
        var registry = new AgentProfileRegistry();
        registry.Register(new AgentProfile
        {
            TargetId = "authored-judge",
            Drafter = FakeArtifactDrafter.OfContent("x"),
            Validators = new[] { authored },
        });
        registry.Resolve("authored-judge").Should().NotBeNull();
    }

    private static byte[] CompileValidatorAssembly()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Nexo.Core.Application.Orchestration;
            using Nexo.Core.Domain.Bricks.Ports;

            public sealed class SelfProducedValidator : IPostValidator<GeneratedArtifact>
            {
                public ValueTask<(bool ok, string? reason)> ValidateAsync(GeneratedArtifact output, CancellationToken ct)
                    => new((true, null));
            }
            """;

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToArray();
        var compilation = CSharpCompilation.Create(
            "JudgeGrowthProbe",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var peStream = new MemoryStream();
        var emit = compilation.Emit(peStream);
        emit.Success.Should().BeTrue(string.Join(" | ", emit.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)));
        return peStream.ToArray();
    }
}
