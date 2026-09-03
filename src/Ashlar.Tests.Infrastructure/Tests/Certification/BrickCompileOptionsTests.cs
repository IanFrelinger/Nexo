using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Testing.CodeAnalysis;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The in-process legs compile the program the BUILD compiled: the unit-level half of
/// <see cref="CompiledProgramParityTests"/>, with no SDK in the loop.
///
/// <para>Each fact takes one leg — the analyzer fence, the mutation catalog, the per-mutant
/// compile — and shows the same source judged two ways: once under Roslyn's defaults (what every
/// leg did before) and once under the options a build would have used. Where the two verdicts
/// differ is exactly where a byte-identical source, and therefore an identical signed content
/// hash, was certifying a program nobody had judged. The reader of the compiler's own record
/// (<see cref="CompiledCompilationOptions"/>) is pinned against a real Roslyn emit, and the
/// options that cannot be honoured are pinned as refusals rather than silent defaults.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class BrickCompileOptionsTests : IDisposable
{
    private static readonly IReadOnlyList<string> BrickReferences =
    [
        typeof(DomainBrick).Assembly.Location,
        typeof(BrickInput).Assembly.Location,
    ];

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ashlar-compile-options-" + Guid.NewGuid().ToString("N"));

    public BrickCompileOptionsTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
            // Best effort; a mapped assembly may still hold the file.
        }
    }

    // ── the analyzer fence ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task The_fence_judges_the_branch_the_build_compiled()
    {
        var gate = new AnalyzerFenceGate();

        var defaults = await gate.EvaluateAsync(GuardedSource("ASHLAR_EVIL"), BrickReferences);
        defaults.Passed.Should().BeTrue(
            "with ASHLAR_EVIL undefined the wall-clock read is disabled text, and that is the verdict the fence used "
            + "to give the built program too: {0}", defaults.FormatProposerFeedback());

        var asBuilt = await gate.EvaluateAsync(
            GuardedSource("ASHLAR_EVIL"), BrickReferences, compileOptions: Options(symbols: ["ASHLAR_EVIL"]));

        asBuilt.Passed.Should().BeFalse("under the build's symbols the guarded DateTime.Now is code");
        var finding = asBuilt.Findings.Should().ContainSingle(f => f.Id == "ASHLAR0006").Subject;
        finding.Line.Should().BeGreaterThan(0, "the finding is in the candidate's own lines, not in injected text");
    }

    [Fact]
    public async Task The_fence_binds_names_through_the_builds_global_usings()
    {
        // The source imports Fake, whose Clock is deterministic. A project-level `global using Clock
        // = System.DateTime;` wins over that import, so the BUILD reads the wall clock while a
        // default in-process compile reads the fake. Only the binding differs; the text does not.
        var gate = new AnalyzerFenceGate();

        var defaults = await gate.EvaluateAsync(AliasSource, BrickReferences);
        defaults.Passed.Should().BeTrue(defaults.FormatProposerFeedback());

        var asBuilt = await gate.EvaluateAsync(
            AliasSource, BrickReferences,
            compileOptions: Options(globalUsings: ["global using Clock = global::System.DateTime;"]));

        asBuilt.Passed.Should().BeFalse("bound as the build bound it, Clock.Now is DateTime.Now");
        asBuilt.Findings.Should().Contain(f => f.Id == "ASHLAR0006");
    }

    // ── the mutation catalog ──────────────────────────────────────────────────────────────

    [Fact]
    public void The_catalog_mutates_the_branch_the_build_compiled()
    {
        // NET8_0 is defined by the SDK for every net8.0 project; no csproj edit is involved.
        var source = GuardedSource("NET8_0");

        var blind = AstMutationCatalog.CollectMutations(source, BrickReferences);
        var sighted = AstMutationCatalog.CollectMutations(source, BrickReferences, Options(symbols: ["NET8_0"]));

        sighted.Count.Should().BeGreaterThan(blind.Count,
            "the guarded `if` exists as a node only when the symbol is defined; a symbol-free parse sees trivia");
        sighted.Select(m => m.Id).Should().Contain(id => id.StartsWith("negate-condition", StringComparison.Ordinal));
        blind.Select(m => m.Id).Should().NotContain(id => id.StartsWith("negate-condition", StringComparison.Ordinal),
            "the pre-fix catalog never saw the guarded statement, which is the hole");
    }

    // ── the per-mutant compile ────────────────────────────────────────────────────────────

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task Mutants_are_compiled_with_the_builds_overflow_checking()
    {
        // The brick catches OverflowException and the witness relies on it (int.MaxValue + 1 -> -1).
        // Its Summary literal is unobserved, so under the build's checked arithmetic that mutant is a
        // genuine survivor. Compiled unchecked, every mutant wraps on the overflow case and fails the
        // witness: the survivor is "killed" by the wrong compile options, and the escape rate lies.
        var witness = new WitnessSpec("checked-sum",
        [
            new WitnessCase(
                new Dictionary<string, object> { ["a"] = 2, ["b"] = 3 },
                new Dictionary<string, object> { ["sum"] = 5 }),
            new WitnessCase(
                new Dictionary<string, object> { ["a"] = int.MaxValue, ["b"] = 1 },
                new Dictionary<string, object> { ["sum"] = -1 }),
        ]);
        var engine = new BrickMutationEngine();

        var asBuilt = await engine.RunAsync(
            CheckedSource, CheckedTypeName, witness, BrickReferences, CancellationToken.None,
            compileOptions: Options(checkOverflow: true));
        var asDefaults = await engine.RunAsync(
            CheckedSource, CheckedTypeName, witness, BrickReferences, CancellationToken.None,
            compileOptions: Options(checkOverflow: false));

        asBuilt.SurvivingMutantIds.Should().Contain(
            id => id.StartsWith("mutate-string-literal", StringComparison.Ordinal),
            "under checked arithmetic the unobserved Summary edit passes the witness and must be reported; survivors were [{0}]",
            string.Join(", ", asBuilt.SurvivingMutantIds));
        asDefaults.SurvivingMutantIds.Should().BeEmpty(
            "compiled unchecked the overflow case wraps instead of throwing, so every mutant fails it — the vacuous "
            + "kill count the legs used to sign");
    }

    // ── the compiler's own record ─────────────────────────────────────────────────────────

    [Fact]
    public void The_compilers_record_of_its_options_is_read_back_exactly()
    {
        var dll = Emit("Recorded",
            CSharpParseOptions.Default
                .WithLanguageVersion(LanguageVersion.CSharp11)
                .WithPreprocessorSymbols("ZED", "ALPHA", "NET8_0"),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true)
                .WithNullableContextOptions(NullableContextOptions.Enable)
                .WithAllowUnsafe(true));

        var options = CompiledCompilationOptions.Read(dll);

        options.LanguageVersion.Should().Be("11.0");
        options.PreprocessorSymbols.Should().Equal(new[] { "ALPHA", "NET8_0", "ZED" },
            "every symbol the compiler was given, sorted, so the record is stable");
        options.CheckOverflow.Should().BeTrue();
        options.Nullable.Should().Be("Enable");
        options.AllowUnsafe.Should().BeTrue();
        options.GlobalUsings.Should().BeEmpty("global usings come from the compiled source, not from this block");
    }

    [Fact]
    public void Options_the_compiler_left_at_their_defaults_read_back_as_defaults()
    {
        var dll = Emit("Plain", CSharpParseOptions.Default, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var options = CompiledCompilationOptions.Read(dll);

        options.CheckOverflow.Should().BeFalse();
        options.Nullable.Should().Be("Disable");
        options.AllowUnsafe.Should().BeFalse();
        options.PreprocessorSymbols.Should().BeEmpty();
        LanguageVersionFacts.TryParse(options.LanguageVersion, out _).Should().BeTrue(
            "the recorded language version must be one the gate can hand back to Roslyn");
    }

    [Fact]
    public void An_assembly_without_the_compilers_record_is_refused_not_defaulted()
    {
        var dll = Path.Combine(_dir, "NoRecord.dll");
        var compilation = CSharpCompilation.Create(
            "NoRecord",
            [CSharpSyntaxTree.ParseText("public static class C { }")],
            RoslynCodeAnalysisService.BuildReferenceSet(null),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using (var pe = File.Create(dll))
            compilation.Emit(pe).Success.Should().BeTrue();

        var act = () => CompiledCompilationOptions.Read(dll);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*compile options*")
            .WithMessage("*no portable debug information*");
    }

    // ── options the legs cannot honour are refusals ───────────────────────────────────────

    [Fact]
    public void A_language_version_this_gates_compiler_does_not_know_is_refused_by_name()
    {
        var act = () => BrickCompilation.ParseOptions(Options(languageVersion: "99.0"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*'99.0'*");
    }

    [Fact]
    public void A_project_alias_for_the_harnesss_own_DomainBrick_name_is_dropped_when_it_names_the_same_type_and_refused_otherwise()
    {
        // The wrapper injects `using DomainBrick = Ashlar.Core.Domain.Bricks.Brick;` into every
        // certification compile. The same alias from the project (the shape the old samples shared)
        // must not turn into CS1537; an alias of that name for anything else changes the program.
        var same = Options(globalUsings: ["global using DomainBrick = global::Ashlar.Core.Domain.Bricks.Brick;"]);
        BrickCompilation.AssertHonourable(same);
        BrickCompilation.CompanionTrees(same).Should().BeEmpty("the duplicate binds nothing new and would not compile");

        var act = () => BrickCompilation.AssertHonourable(
            Options(globalUsings: ["global using DomainBrick = global::System.Text.StringBuilder;"]));

        act.Should().Throw<InvalidOperationException>().WithMessage("*DomainBrick*").WithMessage("*CS1537*");
    }

    // ── the record ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_canonical_form_is_stable_under_reordering_and_is_what_the_record_signs()
    {
        var a = Options(symbols: ["NET8_0", "NET", "TRACE"], globalUsings: ["global using global::System.IO;", "global using global::System;"]);
        var b = Options(symbols: ["TRACE", "NET", "NET8_0"], globalUsings: ["global using global::System;", "global using global::System.IO;"]);

        a.Canonical().Should().Be(b.Canonical());
        a.Canonical().Should().Be(
            "langVersion=12.0;checkOverflow=false;nullable=Disable;unsafe=false;symbols=NET,NET8_0,TRACE;"
            + "globalUsings=global using global::System.IO; global using global::System;");

        var input = a.ToCertificationInput();
        input.Kind.Should().Be(BrickCompileOptions.InputKind);
        input.Id.Should().Be(a.Canonical(), "a reader of the record sees the options, not just a digest");
        input.Hash.Should().Be(Ashlar.Certification.Contracts.BrickContentHasher.ComputeSha256(a.Canonical()));
    }

    // ── fixtures ──────────────────────────────────────────────────────────────────────────

    private static BrickCompileOptions Options(
        string languageVersion = "12.0",
        IReadOnlyList<string>? symbols = null,
        bool checkOverflow = false,
        IReadOnlyList<string>? globalUsings = null) => new()
    {
        LanguageVersion = languageVersion,
        PreprocessorSymbols = symbols ?? Array.Empty<string>(),
        CheckOverflow = checkOverflow,
        GlobalUsings = globalUsings ?? Array.Empty<string>(),
    };

    private string Emit(string name, CSharpParseOptions parse, CSharpCompilationOptions options)
    {
        var dll = Path.Combine(_dir, name + ".dll");
        var pdb = Path.Combine(_dir, name + ".pdb");
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText("public static class C { public static int F(int a, int b) => a + b; }", parse)],
            RoslynCodeAnalysisService.BuildReferenceSet(null),
            options);
        using var peStream = File.Create(dll);
        using var pdbStream = File.Create(pdb);
        var emitted = compilation.Emit(
            peStream, pdbStream,
            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: pdb));
        emitted.Success.Should().BeTrue(string.Join("; ", emitted.Diagnostics));
        return dll;
    }

    private static string GuardedSource(string symbol) => $$"""
        using Ashlar.Core.Domain.Bricks;
        using Ashlar.Core.Domain.Execution;

        namespace Ashlar.Tests.Infrastructure.Certification.Fixtures;

        public sealed class GuardedBrick : DomainBrick
        {
            public GuardedBrick()
            {
                Id = "guarded";
                Name = "Guarded";
                Version = "1.0.0";
                Category = BrickCategory.Analysis;
                Description = "Twice the input.";
                Interface = new BrickInterface
                {
                    Inputs = [new BrickInputDefinition("n", "int", "n")],
                    Outputs = [new BrickOutputDefinition("twice", "int", "twice")]
                };
            }

            public override Task<BrickOutput> ExecuteAsync(
                BrickInput input,
                ImplementationType implementation,
                IExecutionContext context,
                CancellationToken cancellationToken = default)
            {
                var n = input.Get<int>("n");
                var twice = n * 2;
                var output = new BrickOutput();
        #if {{symbol}}
                if (DateTime.Now.Ticks < 0)
                    twice = 0;
        #endif
                output.Set("twice", twice);
                return Task.FromResult(output);
            }
        }
        """;

    private const string AliasSource = """
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

        namespace Ashlar.Tests.Infrastructure.Certification.Fixtures
        {
            public sealed class AliasBrick : DomainBrick
            {
                public AliasBrick()
                {
                    Id = "alias";
                    Name = "Alias";
                    Version = "1.0.0";
                    Category = BrickCategory.Analysis;
                    Description = "Twice the input.";
                    Interface = new BrickInterface
                    {
                        Inputs = [new BrickInputDefinition("n", "int", "n")],
                        Outputs = [new BrickOutputDefinition("twice", "int", "twice")]
                    };
                }

                public override Task<BrickOutput> ExecuteAsync(
                    BrickInput input,
                    ImplementationType implementation,
                    IExecutionContext context,
                    CancellationToken cancellationToken = default)
                {
                    var n = input.Get<int>("n");
                    var twice = n * 2 + Math.Sign(Clock.Now.Ticks) - 1;
                    var output = new BrickOutput();
                    output.Set("twice", twice);
                    return Task.FromResult(output);
                }
            }
        }
        """;

    private const string CheckedTypeName = "Ashlar.Tests.Infrastructure.Certification.Fixtures.CheckedSumBrick";

    private const string CheckedSource = """
        using Ashlar.Core.Domain.Bricks;
        using Ashlar.Core.Domain.Execution;

        namespace Ashlar.Tests.Infrastructure.Certification.Fixtures;

        public sealed class CheckedSumBrick : DomainBrick
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
}
