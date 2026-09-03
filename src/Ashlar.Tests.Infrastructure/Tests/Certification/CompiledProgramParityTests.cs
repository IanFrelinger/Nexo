using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The gate must judge the program the build compiled — not a program that happens to share its
/// source bytes.
///
/// <para>The hole these pin: every in-process leg (the analyzer fence, the mutation catalog, the
/// per-mutant compiles) parsed the brick source with Roslyn's DEFAULT options — no preprocessor
/// symbols, no overflow checking, no global usings — while the real build used the evaluated
/// project. So two projects with a byte-identical <c>Brick.cs</c>, and therefore an identical signed
/// <c>contentHash</c>, compiled different programs: one <c>.csproj</c> defined a symbol that switched
/// a <c>File.WriteAllText</c> backdoor into the assembly, the fence never saw the guarded lines, the
/// mutation leg never mutated them, and BOTH certified ADMIT. The witness leg was the only leg
/// judging the built assembly; the fence and mutation legs judged a different one.</para>
///
/// <para>It is worse than "a malicious csproj": the SDK defines <c>NET</c>, <c>NET8_0</c> and
/// <c>NETCOREAPP</c> for every net8.0 brick, so <c>#if NET8_0</c> reaches the same split from a
/// completely stock project. And <c>&lt;Using Include=... Alias=... /&gt;</c> items change name
/// binding in the compiled program with no visible change to the source at all.</para>
///
/// <para>Every fact here builds a real project through <see cref="BrickCertificationProjectLoader"/>
/// (the .NET SDK and nuget.org, exactly as <c>ShippedSampleCertificationTests</c> assumes) and runs
/// the whole gate, because the claim under test is about the seam between the build and the
/// in-process legs, which no unit of either can exercise alone.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class CompiledProgramParityTests : IDisposable
{
    /// <summary>
    /// A cold restore plus a full build and all five legs, per project — two projects in the twin
    /// fact. Healthy runs finish in seconds each on a warm cache; this is a hang net, not a budget.
    /// </summary>
    private const int BuildAndCertifyTimeout = TestTimeouts.HostTouching;

    private readonly string _root;

    public CompiledProgramParityTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ashlar-program-parity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        Environment.SetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG", null);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // A build server may still hold a handle; the temp root is disposable either way.
        }
    }

    // ── the twin pair: byte-identical source, one csproj defines the symbol ────────────────

    [Fact(Timeout = BuildAndCertifyTimeout)]
    public async Task Twins_with_identical_source_and_one_defined_symbol_no_longer_both_admit()
    {
        // EX and EY: the same Brick.cs to the byte. EY's csproj adds ASHLAR_EVIL to DefineConstants,
        // which compiles a File.WriteAllText into ExecuteAsync. Same contentHash, different program.
        var backdoorFile = Path.Combine(_root, "twin-backdoor.txt");
        var source = TwinSource(symbol: "ASHLAR_EVIL", backdoorFile);

        var clean = await CertifyAsync(WriteBrick("TwinEX", StockProject(), source, TwinWitness));
        var backdoor = await CertifyAsync(WriteBrick(
            "TwinEY",
            StockProject(extraProperties: "<DefineConstants>$(DefineConstants);ASHLAR_EVIL</DefineConstants>"),
            source,
            TwinWitness));

        clean.Request.SourceCode.Should().Be(backdoor.Request.SourceCode,
            "the pair is the attack: identical source text, so identical contentHash");
        clean.Decision.Record.ContentHash.Should().Be(backdoor.Decision.Record.ContentHash);

        clean.Decision.Admitted.Should().BeTrue(
            "the clean twin is an ordinary brick and must still certify; failed '{0}': {1}",
            clean.Decision.FailureCheck, clean.Decision.Record.Reason);

        backdoor.Decision.Admitted.Should().BeFalse(
            "the in-process legs must judge the program the build compiled — with ASHLAR_EVIL defined the guarded "
            + "lines are real code, and a witness that cannot observe a file write cannot vouch for them");

        CompileOptionsInput(backdoor.Decision.Record).Should().Contain("ASHLAR_EVIL",
            "the signed record must say which program was judged, and that program was compiled with the symbol");
        CompileOptionsInput(clean.Decision.Record).Should().NotContain("ASHLAR_EVIL");
    }

    // ── the amplification: the SDK defines the symbol for you ─────────────────────────────

    [Fact(Timeout = BuildAndCertifyTimeout)]
    public async Task A_symbol_the_sdk_defines_for_every_net8_brick_splits_the_program_with_no_csproj_edit()
    {
        // No DefineConstants anywhere: Microsoft.NET.Sdk defines NET8_0 for a net8.0 project all by
        // itself, so #if NET8_0 is live in the build and dead in a default in-process parse.
        var backdoorFile = Path.Combine(_root, "net8-backdoor.txt");
        var outcome = await CertifyAsync(WriteBrick(
            "TwinNET8", StockProject(), TwinSource(symbol: "NET8_0", backdoorFile), TwinWitness));

        outcome.Decision.Admitted.Should().BeFalse(
            "a stock project's implicit framework symbols are part of the compiled program and the legs must judge "
            + "under them; verdict was ADMIT with reason: {0}", outcome.Decision.Record.Reason);
        CompileOptionsInput(outcome.Decision.Record).Should().Contain("NET8_0",
            "the symbols the legs judged under come from the build, not from a hard-coded list");
    }

    // ── name binding: a <Using> item the source never shows ───────────────────────────────

    [Fact(Timeout = BuildAndCertifyTimeout)]
    public async Task A_global_using_alias_from_the_csproj_is_judged_by_the_fence_as_the_build_bound_it()
    {
        // The source declares a deterministic Fake.Clock and imports its namespace. The csproj adds
        // a global using ALIAS `Clock` for System.DateTime, and a using alias wins over a namespace
        // import — so the BUILD binds `Clock.Now` to the wall clock while a default in-process
        // compile binds it to the fake. The fence must see what the build saw: ASHLAR0006.
        var outcome = await CertifyAsync(WriteBrick(
            "TwinAlias",
            StockProject(extraItems: "<Using Include=\"System.DateTime\" Alias=\"Clock\" />"),
            AliasSource,
            TwinWitness));

        outcome.Decision.Admitted.Should().BeFalse(outcome.Decision.Record.Reason);
        outcome.Decision.FailureCheck.Should().Be("analyzer",
            "the fence, not a later leg, must catch the wall-clock read the alias smuggles in; reason: {0}",
            outcome.Decision.Record.Reason);
        outcome.Decision.Record.Reason.Should().Contain("ASHLAR0006");
        CompileOptionsInput(outcome.Decision.Record).Should().Contain("Clock",
            "the record must name the global using the program was bound with");
    }

    // ── checked arithmetic: the build's option, or the mutants lie ────────────────────────

    [Fact(Timeout = BuildAndCertifyTimeout)]
    public async Task A_brick_built_with_overflow_checking_has_its_mutants_compiled_with_overflow_checking()
    {
        // The brick catches OverflowException and the witness relies on it (int.MaxValue + 1 -> -1).
        // Its Summary string is NOT observed by the witness, so the mutant that edits it is a genuine
        // survivor and the correct verdict is REJECT. Compiled UNCHECKED, every mutant wraps on the
        // overflow case and fails the witness — the survivor is "killed" by the wrong compile options
        // and the gate admits a witness with no teeth.
        var outcome = await CertifyAsync(WriteBrick(
            "TwinChecked",
            StockProject(extraProperties: "<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>"),
            CheckedSource,
            CheckedWitness));

        outcome.Decision.Admitted.Should().BeFalse(
            "the Summary literal is unobserved by this witness, so under the build's checked arithmetic its mutant survives");
        outcome.Decision.FailureCheck.Should().Be("mutation", outcome.Decision.Record.Reason);
        outcome.Decision.Record.SurvivingMutantIds.Should().Contain(
            id => id.StartsWith("mutate-string-literal", StringComparison.Ordinal),
            "the survivor is the unobserved Summary edit; survivors were [{0}]",
            string.Join(", ", outcome.Decision.Record.SurvivingMutantIds));
        CompileOptionsInput(outcome.Decision.Record).Should().Contain("checkOverflow=true");
    }

    // ── fixtures ──────────────────────────────────────────────────────────────────────────

    private const string TwinWitness = """
        { "brickId": "fifthway-twins", "cases": [
          { "input": { "baseDamage": 50, "armor": 10 }, "expectedOutput": { "finalDamage": 40 } },
          { "input": { "baseDamage": 100, "armor": 20 }, "expectedOutput": { "finalDamage": 80 } },
          { "input": { "baseDamage": 10, "armor": 50 }, "expectedOutput": { "finalDamage": 0 } },
          { "input": { "baseDamage": 7, "armor": 0 }, "expectedOutput": { "finalDamage": 7 } },
          { "input": { "baseDamage": 3, "armor": 3 }, "expectedOutput": { "finalDamage": 0 } } ] }
        """;

    /// <summary>The /tmp/fifthway FifthBrick, with the backdoor's output path made local to the test.</summary>
    private static string TwinSource(string symbol, string backdoorFile) => $$"""
        using Ashlar.Core.Domain.Bricks;
        using Ashlar.Core.Domain.Execution;

        namespace FifthWay.Twins;

        public sealed class FifthBrick : Brick
        {
            public FifthBrick()
            {
                Id = "fifthway-twins";
                Name = "Fifth Way Twins";
                Version = "1.0.0";
                Category = BrickCategory.Analysis;
                Description = "Damage after armor.";
                Interface = new BrickInterface
                {
                    Inputs =
                    [
                        new BrickInputDefinition("baseDamage", "int", "Base damage"),
                        new BrickInputDefinition("armor", "int", "Armor")
                    ],
                    Outputs = [new BrickOutputDefinition("finalDamage", "int", "Final damage")]
                };
            }

            public override Task<BrickOutput> ExecuteAsync(
                BrickInput input,
                ImplementationType implementation,
                IExecutionContext context,
                CancellationToken cancellationToken = default)
            {
                var baseDamage = input.Get<int>("baseDamage");
                var armor = input.Get<int>("armor");
                var finalDamage = Math.Max(0, baseDamage - armor);
                var output = new BrickOutput { Summary = $"Final damage: {finalDamage}" };
                output.Set("finalDamage", finalDamage);
        #if {{symbol}}
                System.IO.File.WriteAllText(@"{{backdoorFile.Replace("\"", "\"\"")}}",
                    "backdoor ran: HOME=" + (Environment.GetEnvironmentVariable("HOME") ?? "?"));
        #endif
                return Task.FromResult(output);
            }
        }
        """;

    private const string AliasSource = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using Ashlar.Core.Domain.Bricks;
        using Ashlar.Core.Domain.Execution;
        using Fake;

        namespace Fake
        {
            public static class Clock
            {
                public static DateTime Now => new DateTime(2000, 1, 1);
            }
        }

        namespace FifthWay.Twins
        {
            public sealed class AliasBrick : Brick
            {
                public AliasBrick()
                {
                    Id = "fifthway-twins";
                    Name = "Alias Twin";
                    Version = "1.0.0";
                    Category = BrickCategory.Analysis;
                    Description = "Damage after armor.";
                    Interface = new BrickInterface
                    {
                        Inputs =
                        [
                            new BrickInputDefinition("baseDamage", "int", "Base damage"),
                            new BrickInputDefinition("armor", "int", "Armor")
                        ],
                        Outputs = [new BrickOutputDefinition("finalDamage", "int", "Final damage")]
                    };
                }

                public override Task<BrickOutput> ExecuteAsync(
                    BrickInput input,
                    ImplementationType implementation,
                    IExecutionContext context,
                    CancellationToken cancellationToken = default)
                {
                    var baseDamage = input.Get<int>("baseDamage");
                    var armor = input.Get<int>("armor");
                    // Sign(Ticks) is 1 for any clock, so the witness passes whichever Clock this binds
                    // to — and every mutant of the arithmetic is killed by it. Only the BINDING differs.
                    var finalDamage = Math.Max(0, baseDamage - armor) + Math.Sign(Clock.Now.Ticks) - 1;
                    var output = new BrickOutput();
                    output.Set("finalDamage", finalDamage);
                    return Task.FromResult(output);
                }
            }
        }
        """;

    private const string CheckedWitness = """
        { "brickId": "checked-sum", "cases": [
          { "input": { "a": 2, "b": 3 }, "expectedOutput": { "sum": 5 } },
          { "input": { "a": 2147483647, "b": 1 }, "expectedOutput": { "sum": -1 } } ] }
        """;

    private const string CheckedSource = """
        using Ashlar.Core.Domain.Bricks;
        using Ashlar.Core.Domain.Execution;

        namespace FifthWay.Checked;

        public sealed class CheckedSumBrick : Brick
        {
            public CheckedSumBrick()
            {
                Id = "checked-sum";
                Name = "Checked Sum";
                Version = "1.0.0";
                Category = BrickCategory.Analysis;
                Description = "Sum, or -1 on overflow.";
                Interface = new BrickInterface
                {
                    Inputs =
                    [
                        new BrickInputDefinition("a", "int", "Left"),
                        new BrickInputDefinition("b", "int", "Right")
                    ],
                    Outputs = [new BrickOutputDefinition("sum", "int", "Sum")]
                };
            }

            public override Task<BrickOutput> ExecuteAsync(
                BrickInput input,
                ImplementationType implementation,
                IExecutionContext context,
                CancellationToken cancellationToken = default)
            {
                var a = input.Get<int>("a");
                var b = input.Get<int>("b");
                int sum;
                try
                {
                    sum = a + b;
                }
                catch (OverflowException)
                {
                    sum = -1;
                }
                var output = new BrickOutput { Summary = "sum of two ints" };
                output.Set("sum", sum);
                return Task.FromResult(output);
            }
        }
        """;

    private static string StockProject(string extraProperties = "", string extraItems = "") => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
            <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
            <NoWarn>NU1701;NU1604</NoWarn>
            {extraProperties}
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.1" />
            {extraItems}
          </ItemGroup>
        </Project>
        """;

    /// <summary>
    /// One brick directory per fact, with a project name unique to this process: the assembly takes
    /// its name from the project, and a second load of the same simple name fails with "assembly with
    /// same name is already loaded", which reads like a gate refusal and is not one.
    /// </summary>
    private string WriteBrick(string label, string csproj, string source, string witness)
    {
        var name = $"{label}_{Guid.NewGuid().ToString("N")[..8]}";
        var dir = Path.Combine(_root, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, name + ".csproj"), csproj);
        File.WriteAllText(Path.Combine(dir, "Brick.cs"), source);
        File.WriteAllText(Path.Combine(dir, "brick.witness.json"), witness);
        return dir;
    }

    private static async Task<(CertificationRequest Request, CertificationDecision Decision)> CertifyAsync(string brickDir)
    {
        // A refusal here is the loader saying the fixture cannot even be loaded; let its designed
        // message surface as the failure rather than wrapping it.
        var request = await BrickCertificationProjectLoader.LoadAsync(
            brickDir, Path.Combine(brickDir, "brick.witness.json")).ConfigureAwait(false);
        var decision = await new CertificationGate(new CertificationRecordSigner())
            .CertifyAsync(request).ConfigureAwait(false);
        return (request, decision);
    }

    /// <summary>The record's signed <c>compile-options</c> input, or a failure naming its absence.</summary>
    private static string CompileOptionsInput(CertificationRecord record)
    {
        var input = record.Inputs.FirstOrDefault(i => i.Kind == "compile-options");
        input.Should().NotBeNull(
            "the signed record must carry the compile options the program was judged under; inputs were [{0}]",
            string.Join(", ", record.Inputs.Select(i => i.Kind)));
        return input!.Id;
    }
}
