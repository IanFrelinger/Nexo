using System.Reflection;
using FluentAssertions;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The signed record says WHO produced it and WHAT it was judged with: the gate assembly and its
/// build, the mutation catalog's version, and — when there was no build to match — that the
/// compile options were the defaults, said outright rather than left blank.
///
/// <para><b>Why.</b> A certificate is a claim that a particular certifier, running a particular
/// catalog, found no surviving mutant. Two records with identical mutant counts mean different
/// things if one came from a catalog with five operator kinds and the other from one with none;
/// and a record silently missing its <c>compile-options</c> input reads as "not recorded" when the
/// truth is "compiled under the defaults because the candidate had no build".
/// <c>gatesPassed[].configuration</c> and <c>inputs[]</c> are already under the signature, so no
/// schema bump is needed — and a record re-labelled to another certifier or catalog must stop
/// verifying.</para>
///
/// <para>One certification (the mutation probe brick, admitted) is shared by the tests through
/// <see cref="ProbeCertification"/>.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class CertifierIdentityRecordTests : IClassFixture<CertifierIdentityRecordTests.ProbeCertification>
{
    private readonly ProbeCertification _probe;

    public CertifierIdentityRecordTests(ProbeCertification probe) => _probe = probe;

    [Fact(Timeout = TestTimeouts.Stress)]
    public Task AnAdmittedRecord_NamesTheCertifierAndTheMutationCatalog_UnderTheSignature()
    {
        var record = _probe.Decision.Record;
        _probe.Decision.Admitted.Should().BeTrue(record.Reason);

        // Computed independently of the gate: the informational version (or the assembly version when
        // none is stamped), the revision after '+' when SourceLink stamped one, and the module version
        // id — which a Deterministic build makes a fingerprint of the exact binary, so the identity
        // is meaningful even in a dev build whose version is the SDK default 1.0.0.
        var assembly = typeof(CertificationGate).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version!.ToString();
        var version = informational.Split('+')[0];
        var mvid = typeof(CertificationGate).Module.ModuleVersionId.ToString("N");
        var identity = CertificationGate.CertifierIdentity;
        identity.Should().StartWith($"certifier=Ashlar.Infrastructure/{version};sourceRevision=")
            .And.EndWith($";certifierMvid={mvid}");
        identity.Should().Match(informational.Contains('+')
            ? $"*;sourceRevision={informational.Split('+')[1]};*"
            : "*;sourceRevision=unstamped;*",
            "the revision is the one the build stamped, or the record says none was");

        foreach (var name in new[] { "correctness-witness", "mutation-gate", "determinism" })
        {
            var pass = record.GatesPassed.Should().ContainSingle(g => g.Name == name).Subject;
            pass.Configuration.Should().Contain(identity, "{0} says who ran it", name);
        }
        record.GatesPassed.Single(g => g.Name == "mutation-gate").Configuration.Should()
            .Contain($"mutationCatalog={AstMutationCatalog.CatalogVersion}",
                "the mutation pass names the catalog version its mutants came from");

        _probe.Signer.Verify(record).Should().BeTrue("the record verifies as minted");
        _probe.Signer.Verify(Relabel(record, $"mutationCatalog={AstMutationCatalog.CatalogVersion}", "mutationCatalog=0"))
            .Should().BeFalse("a record claiming another catalog version is a different signed payload");
        _probe.Signer.Verify(Relabel(record, identity, "certifier=Somebody.Else/9.9.9;sourceRevision=deadbeef;certifierMvid=0"))
            .Should().BeFalse("a record re-attributed to another certifier is a different signed payload");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.Stress)]
    public Task ARequestWithNoBuildBehindIt_SaysSoInTheCompileOptionsInput()
    {
        // The probe request carries no BrickCompileOptions: the brick is an in-process type, like a
        // hot-swap generation or a generated candidate — there is no build whose options the legs
        // could match, so they compiled under the defaults.
        _probe.Request.CompileOptions.Should().BeNull();
        var record = _probe.Decision.Record;

        var input = record.Inputs.Should().ContainSingle(i => i.Kind == "compile-options",
            "absence reads as 'not recorded'; the record must say the defaults were used and why (inputs: {0})",
            string.Join(", ", record.Inputs.Select(i => i.Kind))).Subject;
        input.Id.Should().Be(CertificationGate.NoBuildCompileOptionsId).And.Be("default;reason=no-build",
            "the literal is part of the record format consumers read; renaming it is a record change");
        input.Hash.Should().Be(BrickContentHasher.ComputeSha256(input.Id),
            "hashed like every other synthesised input, so a verifier can recompute it");
        record.Inputs[0].Kind.Should().Be("witness", "the witness input still comes first");
        return Task.CompletedTask;
    }

    private static CertificationRecord Relabel(CertificationRecord record, string from, string to) => record with
    {
        GatesPassed = record.GatesPassed
            .Select(g => g with { Configuration = g.Configuration?.Replace(from, to, StringComparison.Ordinal) })
            .ToArray(),
    };

    /// <summary>One admitted certification of the mutation probe brick, minted with a known key.</summary>
    public sealed class ProbeCertification : IAsyncLifetime
    {
        private const string HmacKey = "certifier-identity-test-hmac";

        private const string ProbeLog =
            "2024-01-01 INFO Started\n2024-01-01 ERROR First failure: connection reset\n2024-01-01 WARN Retrying\n2024-01-01 ERROR Second failure: timeout";

        private static readonly WitnessSpec StrongWitness = new(
            "mutation-probe-brick",
            [
                new WitnessCase(
                    new Dictionary<string, object> { ["logText"] = ProbeLog },
                    new Dictionary<string, object>
                    {
                        ["errorCount"] = 2,
                        ["firstErrorMessage"] = "First failure: connection reset"
                    }),
                MutationProbeWitnesses.ZeroErrorCase
            ]);

        public CertificationRecordSigner Signer { get; } = new(HmacKey);

        public CertificationRequest Request { get; private set; } = null!;

        public CertificationDecision Decision { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            Request = new CertificationRequest
            {
                Brick = new MutationProbeBrick(),
                Witness = StrongWitness,
                SourceCode = MutationProbeBrickSource.Code,
                ProjectPath = CreateCleanProjectFile(),
                CompilationReferences =
                [
                    typeof(DomainBrick).Assembly.Location,
                    typeof(BrickInput).Assembly.Location,
                    typeof(MutationProbeBrick).Assembly.Location,
                ],
                BrickTypeName = typeof(MutationProbeBrick).FullName,
            };
            Decision = await new CertificationGate(Signer).CertifyAsync(Request);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private static string CreateCleanProjectFile()
        {
            var path = Path.Combine(Path.GetTempPath(), $"ashlar-cert-identity-{Guid.NewGuid():N}.csproj");
            File.WriteAllText(path, """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.0" />
    <PackageReference Include="Ashlar.Authoring" Version="0.1.0" />
  </ItemGroup>
</Project>
""");
            return path;
        }
    }
}
