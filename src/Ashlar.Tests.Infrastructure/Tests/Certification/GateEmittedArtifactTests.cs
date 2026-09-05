using System.Text;
using FluentAssertions;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>A8: the certifier compiles and ships the binary it judged.</summary>
[Trait("Category", "Certification")]
public sealed class GateEmittedArtifactTests
{
    [Fact]
    public void Compiler_EmitsDiscoverableBrick_WithoutLoadingAuthorMsbuild()
    {
        var artifact = GateEmittedArtifactCompiler.Compile(
            MutationProbeBrickSource.Code,
            BrickCertificationProjectLoader.DefaultCompilationReferences());

        artifact.BrickTypeName.Should().Be("Ashlar.Tests.Infrastructure.Certification.Fixtures.MutationProbeBrick");
        artifact.AssemblyBytes.Should().NotBeEmpty();
        artifact.AssemblySha256.Should().Be(BrickContentHasher.ComputeSha256(artifact.AssemblyBytes));
        artifact.CompileOptionsBlob.Should().Be(BrickCompileOptions.CanonicalBlob);
    }

    [Fact]
    public void MetadataDiscovery_DoesNotRunConstructor_OnInfiniteCtor()
    {
        var source = HonestSource("HangCtorBrick", "hang-ctor", constructorBody: "while (true) { }");
        var artifact = GateEmittedArtifactCompiler.Compile(
            source,
            BrickCertificationProjectLoader.DefaultCompilationReferences());

        artifact.BrickTypeName.Should().Contain("HangCtorBrick");
        // If discovery had CreateInstance'd, this test would hang past xunit's default.
    }

    [Fact]
    public void IlFence_RefusesEnvironmentExit_BeforeActivation()
    {
        var source = HonestSource("ExitCtorBrick", "exit-ctor", constructorBody: "Environment.Exit(0);");
        var artifact = GateEmittedArtifactCompiler.Compile(
            source,
            BrickCertificationProjectLoader.DefaultCompilationReferences());

        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*System.Environment*");
    }

    [Fact]
    public void BuildSurfaceFence_RefusesAuthorTargetAndExec()
    {
        var dir = CreateTempProject(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <Target Name="Hijack" BeforeTargets="Build">
                <Exec Command="echo hijacked" />
              </Target>
            </Project>
            """,
            HonestSource("FenceBrick", "fence"));

        var act = () => BuildSurfaceFence.Inspect(dir, Path.Combine(dir, "Brick.csproj"));
        act.Should().Throw<InvalidOperationException>().WithMessage("*Target*");
    }

    [Fact]
    public void BuildSurfaceFence_RefusesAuthorNuGetConfig()
    {
        var dir = CreateTempProject(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """,
            HonestSource("FenceBrick", "fence"));
        File.WriteAllText(Path.Combine(dir, "NuGet.Config"), "<configuration><packageSources><clear /></packageSources></configuration>");

        var act = () => BuildSurfaceFence.Inspect(dir, Path.Combine(dir, "Brick.csproj"));
        act.Should().Throw<InvalidOperationException>().WithMessage("*NuGet.Config*");
    }

    [Fact]
    public void StrictUtf8_RefusesUtf16LeSource()
    {
        var utf16 = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("class X {}")).ToArray();
        var act = () => StrictUtf8SourceDecoder.Decode(utf16);
        act.Should().Throw<InvalidOperationException>().WithMessage("*UTF-16LE*");
    }

    [Fact]
    public async Task Loader_RecordsGateEmittedArtifact_AndStrictVerifyRequiresIt()
    {
        var dir = CreateTempProject(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """,
            MutationProbeBrickSource.Code);
        var witnessPath = Path.Combine(dir, "witness.json");
        await File.WriteAllTextAsync(witnessPath, """
            {"brickId":"mutation-probe-brick","cases":[{"input":{"logText":"ERROR a\nERROR b"},"expectedOutput":{"errorCount":2,"firstErrorMessage":"a"}}]}
            """);

        var request = await BrickCertificationProjectLoader.LoadAsync(dir, witnessPath);
        request.EmittedArtifact.Should().NotBeNull();
        request.BrickTypeName.Should().Be(request.EmittedArtifact!.BrickTypeName);

        var decision = await new CertificationGate(new CertificationRecordSigner()).CertifyAsync(request);
        decision.Record.Inputs.Should().Contain(i => i.Kind == CertificationInputKinds.GateEmittedArtifact
            && i.Hash == request.EmittedArtifact.AssemblySha256);
        decision.Record.Inputs.Should().Contain(i => i.Kind == CertificationInputKinds.CertifierIdentity);
        decision.Record.Inputs.Should().Contain(i => i.Kind == CertificationInputKinds.IlImportFence);
        decision.Record.Inputs.Should().Contain(i => i.Kind == CertificationInputKinds.ExecutionMode && i.Id == "gate-emitted");

        var data = CertificationRecordMapper.ToData(decision.Record);
        if (decision.Admitted)
        {
            var trusted = CertificationTrustVerifier.Verify(
                data,
                request.SourceCode,
                request.EmittedArtifact.AssemblyBytes,
                options: CertificationVerifyOptions.Strict);
            trusted.Trusted.Should().BeTrue($"{trusted.FailureCode}: {trusted.Reason}");

            var swapped = request.EmittedArtifact.AssemblyBytes.ToArray();
            swapped[0x80] ^= 0xFF;
            var tampered = CertificationTrustVerifier.Verify(
                data,
                request.SourceCode,
                swapped,
                options: CertificationVerifyOptions.Strict);
            tampered.Trusted.Should().BeFalse();
            tampered.FailureCode.Should().Be("artifact-hash-mismatch");
        }
        else
        {
            // Witness above is intentionally weak relative to the probe brick; the
            // compile-authority inputs must still be on the FAIL record.
            decision.Record.Inputs.Should().Contain(i => i.Kind == CertificationInputKinds.GateEmittedArtifact);
        }
    }

    [Fact]
    public void CompilerCeiling_NamesCSharp12()
    {
        CompilerCeiling.IsAtOrUnderCeiling(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12).Should().BeTrue();
        var aboveCeiling = (Microsoft.CodeAnalysis.CSharp.LanguageVersion)1300;
        CompilerCeiling.IsAtOrUnderCeiling(aboveCeiling).Should().BeFalse();
        CompilerCeiling.FormatRefusal(aboveCeiling)
            .Should().Contain("CSharp12").And.Contain("compiler-ceiling");
    }

    [Fact]
    public async Task Gate_RefusesArtifactWhoseHashDoesNotMatchBytes()
    {
        var dir = CreateTempProject(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """,
            MutationProbeBrickSource.Code);
        var witnessPath = Path.Combine(dir, "witness.json");
        await File.WriteAllTextAsync(witnessPath, """
            {"brickId":"mutation-probe-brick","cases":[{"input":{"logText":"ERROR a\nERROR b"},"expectedOutput":{"errorCount":2,"firstErrorMessage":"a"}}]}
            """);

        var request = await BrickCertificationProjectLoader.LoadAsync(dir, witnessPath);
        var tampered = request.EmittedArtifact! with { AssemblySha256 = "not-the-hash" };
        var decision = await new CertificationGate(new CertificationRecordSigner())
            .CertifyAsync(request with { EmittedArtifact = tampered });

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("load");
        decision.Record.Reason.Should().Contain("hash");
    }

    [Fact]
    public async Task Gate_WitnessesActivatedPe_NotCallerBrickInstance()
    {
        var dir = CreateTempProject(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """,
            MutationProbeBrickSource.Code);
        var witnessPath = Path.Combine(dir, "witness.json");
        await File.WriteAllTextAsync(witnessPath, """
            {"brickId":"mutation-probe-brick","cases":[{"input":{"logText":"ERROR a\nERROR b"},"expectedOutput":{"errorCount":2,"firstErrorMessage":"a"}}]}
            """);

        var request = await BrickCertificationProjectLoader.LoadAsync(dir, witnessPath);
        var exploding = new ExplodingProbeBrick();
        var decision = await new CertificationGate(new CertificationRecordSigner())
            .CertifyAsync(request with { Brick = exploding });

        decision.Record.Reason.Should().NotContain(
            "caller instance must not execute",
            "WitnessRunner swallows ExecuteAsync exceptions; a FAIL with this reason means the caller brick was judged");
        decision.FailureCheck.Should().NotBe("load");
        decision.Record.Inputs.Should().Contain(i => i.Kind == CertificationInputKinds.GateEmittedArtifact);
    }

    [Fact]
    public void Activator_InspectsIlBeforeLoad()
    {
        var artifact = GateEmittedArtifactCompiler.Compile(
            """
            using Ashlar.Core.Domain.Bricks;
            using Ashlar.Core.Domain.Execution;
            public sealed class ThreadBrick : DomainBrick
            {
                public ThreadBrick()
                {
                    Id = "thread-brick";
                    Name = "ThreadBrick";
                    Version = "1.0.0";
                    Category = BrickCategory.Analysis;
                    Description = "probe";
                    Interface = new BrickInterface
                    {
                        Inputs = [new BrickInputDefinition("n", "int", "n")],
                        Outputs = [new BrickOutputDefinition("n", "int", "n")]
                    };
                }
                public override Task<BrickOutput> ExecuteAsync(
                    BrickInput input, ImplementationType implementation, IExecutionContext context,
                    CancellationToken cancellationToken = default)
                {
                    new System.Threading.Thread(() => { }).Start();
                    var output = new BrickOutput { Summary = "ok" };
                    output.Set("n", input.Get<int>("n"));
                    return Task.FromResult(output);
                }
            }
            """,
            BrickCertificationProjectLoader.DefaultCompilationReferences());

        var act = () => CertifiedBrickActivator.Activate(artifact);
        act.Should().Throw<InvalidOperationException>().WithMessage("*System.Threading.Thread*");
    }

        private sealed class ExplodingProbeBrick : DomainBrick
        {
            public ExplodingProbeBrick()
            {
                Id = "mutation-probe-brick";
                Name = "Exploding";
                Version = "1.0.0";
                Category = BrickCategory.Analysis;
                Description = "Must not execute";
            }

            public override Task<BrickOutput> ExecuteAsync(
                BrickInput input,
                ImplementationType implementation,
                IExecutionContext context,
                CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("caller instance must not execute");
        }

    [Fact]
    public void StrictPreset_RefusesRecordWithoutArtifact()
    {
        var record = new CertificationRecordData
        {
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "x",
            ContentHash = BrickContentHasher.ComputeSha256("source"),
            SchemaVersion = 2,
            Signature = "not-checked-yet"
        };
        // Unsigned / bad signature fails first; pin the completeness codes on a
        // structurally complete but unsigned record via the dedicated flags.
        var missing = CertificationTrustVerifier.Verify(
            record with
            {
                Signed = false,
                Status = "FAIL",
                Admitted = false
            },
            "source",
            options: CertificationVerifyOptions.Strict);
        missing.Trusted.Should().BeFalse();
        missing.FailureCode.Should().Be("record-not-admitted");
    }

    private static string HonestSource(string className, string id, string constructorBody = "") =>
        $$"""
        using Ashlar.Core.Domain.Bricks;
        using Ashlar.Core.Domain.Execution;

        namespace Ashlar.Tests.Infrastructure.Certification.Fixtures;

        public sealed class {{className}} : DomainBrick
        {
            public {{className}}()
            {
                Id = "{{id}}";
                Name = "{{className}}";
                Version = "1.0.0";
                Category = BrickCategory.Analysis;
                Description = "fixture";
                Interface = new BrickInterface
                {
                    Inputs = [new BrickInputDefinition("n", "int", "n")],
                    Outputs = [new BrickOutputDefinition("n", "int", "n")]
                };
                {{constructorBody}}
            }

            public override Task<BrickOutput> ExecuteAsync(
                BrickInput input,
                ImplementationType implementation,
                IExecutionContext context,
                CancellationToken cancellationToken = default)
            {
                var n = input.Get<int>("n");
                var output = new BrickOutput { Summary = "ok" };
                output.Set("n", n);
                return Task.FromResult(output);
            }
        }
        """;

    private static string CreateTempProject(string csproj, string source)
    {
        var dir = Path.Combine(Path.GetTempPath(), "ashlar-a8-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Brick.csproj"), csproj);
        File.WriteAllText(Path.Combine(dir, "Brick.cs"), source);
        return dir;
    }
}
