using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Certification;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Where the loader gets the assemblies it re-compiles the brick against.
///
/// <para>It used to glob <c>*.dll</c> out of the build output directory. The SDK does not copy
/// package assemblies into a library's output — <c>CopyLocalLockFileAssemblies</c> is off by
/// default — so a stock brick that referenced <c>Ashlar.Authoring</c>, the exact shape
/// <c>ashlar new brick</c> scaffolds, built to an output holding only itself, and the analyzer
/// fence refused every one of them with "analyzer anchor type 'Ashlar.Core.Domain.Bricks.Brick' is
/// not resolvable" until the author added an MSBuild property that the docs filed under "things
/// that will bite you". Neither shipped sample and not the template set it: the scaffold could not
/// be certified as scaffolded.</para>
///
/// <para>These facts pin the replacement: the reference set is the one the COMPILER recorded in
/// the PDB, located through the paths MSBuild reports from the same build, joined by MVID — and
/// anything that cannot be joined is a refusal by name, never a fallback to a partial set.</para>
/// </summary>
[Trait("Category", "Certification")]
[Trait("Category", "SlowTier")]
public sealed class BrickCertificationProjectLoaderReferenceTests : IDisposable
{
    private readonly string _dir;

    /// <summary>
    /// Unique per test: the assembly takes the project's name, and the tests that reach
    /// <c>Assembly.LoadFrom</c> share one process, where a second assembly of the same name fails
    /// to load in a way that looks like a refusal and is not one.
    /// </summary>
    private readonly string _projectName = "RefBrick" + Guid.NewGuid().ToString("N")[..8];

    public BrickCertificationProjectLoaderReferenceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-loader-refs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    // ── the defect: a stock package-referencing brick, no CopyLocalLockFileAssemblies ──────

    [Fact]
    public async Task The_brick_template_certifies_exactly_as_scaffolded()
    {
        // The real template files, with the scaffolder's tokens substituted and NOTHING else —
        // in particular no <CopyLocalLockFileAssemblies>. If this fails, `ashlar new brick`
        // produces a brick the gate cannot certify, whatever the docs say.
        var templateDir = Path.Combine(TestPaths.FindRepoRoot(), "samples", "templates", "brick", "__BrickName__Brick");
        var csproj = Substitute(File.ReadAllText(Path.Combine(templateDir, "__BrickName__Brick.csproj")));
        var source = Substitute(File.ReadAllText(Path.Combine(templateDir, "__BrickName__Brick.cs")));
        csproj.Should().NotContain("CopyLocalLockFileAssemblies",
            "the template must certify without the workaround, or the workaround belongs back in it");

        File.WriteAllText(Path.Combine(_dir, _projectName + "Brick.csproj"), csproj);
        File.WriteAllText(Path.Combine(_dir, _projectName + "Brick.cs"), source);
        Write("witness.json", """
            {
              "brickId": "acme.reference-brick",
              "cases": [
                { "input": { "name": "Ada" },
                  "expectedOutput": { "message": "Hello, Ada!", "implementation": "Deterministic",
                                      "$summary": "Generated greeting for Ada." } },
                { "input": { "name": "" },
                  "expectedOutput": { "message": "Hello, !", "implementation": "Deterministic",
                                      "$summary": "Generated greeting for ." } }
              ]
            }
            """);

        var request = await BrickCertificationProjectLoader.LoadAsync(_dir, Path.Combine(_dir, "witness.json"));

        request.CompilationReferences.Should().Contain(
            p => Path.GetFileName(p).Equals("Ashlar.Core.Domain.dll", StringComparison.OrdinalIgnoreCase),
            "the assembly that defines the analyzer anchor type must be among the references the compiler used");
        request.CompilationReferences.Should().OnlyContain(p => File.Exists(p));

        var decision = await new CertificationGate(new CertificationRecordSigner()).CertifyAsync(request);

        decision.Admitted.Should().BeTrue($"REJECTED at {decision.FailureCheck}: {decision.Record.Reason}");
        decision.Record.EscapeRate.Should().Be(0);
    }

    [Fact]
    public async Task The_reference_set_is_the_compilers_not_the_output_directorys()
    {
        // With copy-local off the output directory holds the brick alone. The package assemblies
        // the fence needs live in the NuGet cache, and that is where the loader must find them.
        WriteStockBrick();
        Write("witness.json", """{"brickId":"stock","cases":[]}""");

        var request = await BrickCertificationProjectLoader.LoadAsync(_dir, Path.Combine(_dir, "witness.json"));

        var names = request.CompilationReferences.Select(Path.GetFileName).ToList();
        names.Should().Contain("Ashlar.Core.Domain.dll")
            .And.Contain("Ashlar.Brick.Contracts.dll")
            .And.Contain("Ashlar.Authoring.dll");
        names.Should().NotContain("System.Runtime.dll",
            "the targeting pack's reference assemblies are verified but withheld: the in-process compilation "
            + "supplies the host runtime's framework, and a second core library makes every predefined type "
            + "unresolvable (CS0518) — observed on the first run of the fix");
        request.CompilationReferences.Should().OnlyContain(p => File.Exists(p));
        request.CompilationReferences.Should().OnlyContain(
            p => !p.Contains(Path.Combine("runtimes", ""), StringComparison.OrdinalIgnoreCase)
                 || !p.Contains(Path.DirectorySeparatorChar + "native" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "LLamaSharp's natives are in Ashlar.Authoring's runtime graph and must never reach the compiler");
    }

    // ── refusals, by name ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task A_reference_removed_from_the_build_report_after_the_compile_is_refused_by_name()
    {
        // The reference list MSBuild reports is post-build state the project's own targets can
        // edit. The compiler's record cannot be. Scrub one assembly from the list after
        // CoreCompile and the two disagree — which must be a refusal naming that assembly, not a
        // fence failure blaming the candidate, and not a silent compile against fewer references.
        WriteStockBrick("""
            <Target Name="Scrub" AfterTargets="CoreCompile">
              <ItemGroup>
                <ReferencePathWithRefAssemblies Remove="@(ReferencePathWithRefAssemblies->WithMetadataValue('Filename', 'Ashlar.Core.Domain'))" />
              </ItemGroup>
            </Target>
            """);
        Write("witness.json", """{"brickId":"stock","cases":[]}""");

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(_dir, Path.Combine(_dir, "witness.json"));

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("Ashlar.Core.Domain.dll")
            .And.Contain("ReferencePathWithRefAssemblies")
            .And.Contain("Fix:")
            .And.NotContain("anchor type", "the loader must refuse before the fence has anything to blame the candidate for");
    }

    [Fact]
    public async Task Following_the_fix_the_reference_refusal_names_clears_it()
    {
        // The refusal says: remove the target that edits ReferencePathWithRefAssemblies after the
        // compile. This is that project with the target removed, and it must load — the fix a
        // refusal names has to be one the author can actually carry out.
        WriteStockBrick();
        Write("witness.json", """{"brickId":"stock","cases":[]}""");

        var request = await BrickCertificationProjectLoader.LoadAsync(_dir, Path.Combine(_dir, "witness.json"));

        request.CompilationReferences.Select(Path.GetFileName).Should().Contain("Ashlar.Core.Domain.dll");
    }

    [Fact]
    public async Task A_build_whose_reference_list_was_emptied_is_refused_not_globbed()
    {
        // The fail-closed half. No reported references is not "no references": it is "the gate
        // cannot locate them", and the answer is a refusal, never a fall back to whatever the
        // output directory happens to hold.
        WriteStockBrick("""
            <Target Name="ScrubAll" AfterTargets="CoreCompile">
              <ItemGroup>
                <ReferencePathWithRefAssemblies Remove="@(ReferencePathWithRefAssemblies)" />
              </ItemGroup>
            </Target>
            """);
        Write("witness.json", """{"brickId":"stock","cases":[]}""");

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(_dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("no compiler references")
            .And.Contain("ReferencePathWithRefAssemblies")
            .And.Contain("Fix:");
    }

    // ── the join itself, without a build ──────────────────────────────────────────────────

    [Fact]
    public void A_reference_the_compiler_recorded_that_no_reported_file_carries_is_refused_by_name()
    {
        var dependency = Copy(typeof(System.Linq.Enumerable).Assembly.Location, "Dependency.dll");

        var act = () => BrickCertificationProjectLoader.ResolveCompilerReferences(
            "Brick.csproj",
            [dependency],
            [
                Recorded(dependency),
                new CompiledMetadataReference("Ashlar.Core.Domain.dll", Guid.NewGuid(), true, false)
            ],
            Copy(typeof(object).Assembly.Location, "Brick.dll"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("Ashlar.Core.Domain.dll")
            .And.Contain("removed from ReferencePathWithRefAssemblies")
            .And.Contain("Fix:");
    }

    [Fact]
    public void An_assembly_swapped_under_a_reported_path_is_refused_as_a_different_module()
    {
        // Same file name, same path, different module: a reference replaced after the compile.
        // Matching by name would hand Roslyn the substitute; matching by MVID refuses it and says
        // which way the check failed.
        var swapped = Copy(typeof(System.Linq.Enumerable).Assembly.Location, "Ashlar.Core.Domain.dll");

        var act = () => BrickCertificationProjectLoader.ResolveCompilerReferences(
            "Brick.csproj",
            [swapped],
            [new CompiledMetadataReference("Ashlar.Core.Domain.dll", Guid.NewGuid(), true, false)],
            Copy(typeof(object).Assembly.Location, "Brick.dll"));

        act.Should().Throw<InvalidOperationException>()
            .And.Message.Should().Contain("DIFFERENT module")
            .And.Contain(swapped)
            .And.Contain("Fix:");
    }

    [Fact]
    public void A_recorded_reference_whose_reported_file_is_not_a_managed_assembly_is_refused_saying_so()
    {
        var junk = Path.Combine(_dir, "Ashlar.Core.Domain.dll");
        File.WriteAllBytes(junk, [0x7F, 0x45, 0x4C, 0x46, 0x02, 0x01, 0x01, 0x00]);

        var act = () => BrickCertificationProjectLoader.ResolveCompilerReferences(
            "Brick.csproj",
            [junk],
            [new CompiledMetadataReference("Ashlar.Core.Domain.dll", Guid.NewGuid(), true, false)],
            Copy(typeof(object).Assembly.Location, "Brick.dll"));

        act.Should().Throw<InvalidOperationException>()
            .And.Message.Should().Contain("not a managed assembly")
            .And.Contain(junk);
    }

    [Fact]
    public void A_reported_path_the_compiler_did_not_record_is_not_handed_on()
    {
        // Roslyn is meant to see what csc saw and nothing else. An extra item in the post-build
        // list is not a reason to refuse — it is not a reference — but it must not be passed on.
        var recorded = Copy(typeof(System.Linq.Enumerable).Assembly.Location, "Recorded.dll");
        var extra = Copy(typeof(Console).Assembly.Location, "Extra.dll");
        var primary = Copy(typeof(object).Assembly.Location, "Brick.dll");

        var references = BrickCertificationProjectLoader.ResolveCompilerReferences(
            "Brick.csproj", [recorded, extra], [Recorded(recorded)], primary);

        references.Should().Equal(primary, recorded);
    }

    [Fact]
    public void The_compilers_record_is_read_out_of_a_real_pdb()
    {
        // Pins the blob layout against an assembly csc actually produced: this test assembly's
        // own Ashlar.Infrastructure.dll, built with a portable PDB beside it.
        var recorded = CompiledMetadataReferences.Read(typeof(BrickCertificationProjectLoader).Assembly.Location);

        recorded.Should().NotBeEmpty();
        recorded.Select(r => r.FileName).Should().Contain("System.Runtime.dll")
            .And.Contain("Ashlar.Core.Domain.dll");
        recorded.Should().OnlyContain(r => r.Mvid != Guid.Empty && r.IsAssembly);
    }

    [Fact]
    public void An_assembly_with_no_debug_record_of_its_references_is_refused()
    {
        // A copy in a directory with no PDB: the debug directory names a file that is not there.
        var orphan = Copy(typeof(object).Assembly.Location, "Orphan.dll");

        var act = () => CompiledMetadataReferences.Read(orphan);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("compiled against").And.Contain("Fix:");
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────

    private string Substitute(string text) => text
        .Replace("__BrickName__", _projectName)
        .Replace("__DisplayName__", "Reference Brick")
        .Replace("__BrickId__", "acme.reference-brick")
        .Replace("__Namespace__", "Acme.Bricks")
        .Replace("__AshlarVersion__", "0.1.1");

    /// <summary>
    /// The shape <c>ashlar new brick</c> emits, reduced to what matters here: one Ashlar package
    /// reference, no CopyLocalLockFileAssemblies, and — because this is the default — no package
    /// assembly in the build output.
    /// </summary>
    private void WriteStockBrick(string extraProjectXml = "")
    {
        File.WriteAllText(Path.Combine(_dir, _projectName + ".csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Ashlar.Authoring" Version="0.1.1" />
              </ItemGroup>
              {extraProjectXml}
            </Project>
            """);
        Write(_projectName + ".cs", $$"""
            using Ashlar.Core.Domain.Bricks;
            using Ashlar.Core.Domain.Execution;

            namespace Stock;

            public sealed class {{_projectName}} : Brick
            {
                public {{_projectName}}()
                {
                    Id = "stock.brick";
                    Name = "Stock";
                    Version = "1.0.0";
                    Category = BrickCategory.Transform;
                    Description = "A stock brick.";
                    Interface = new BrickInterface
                    {
                        Inputs = [new BrickInputDefinition("name", "string", "Name", required: false, defaultValue: "world")],
                        Outputs = [new BrickOutputDefinition("message", "string", "Greeting")]
                    };
                }

                public override Task<BrickOutput> ExecuteAsync(
                    BrickInput input,
                    ImplementationType implementation,
                    IExecutionContext context,
                    CancellationToken cancellationToken = default)
                {
                    var name = input.Get<string>("name");
                    var output = new BrickOutput { Summary = "Greeted." };
                    output.Set("message", $"Hello, {name}!");
                    return Task.FromResult(output);
                }
            }
            """);
    }

    private void Write(string relativePath, string content)
    {
        var full = Path.Combine(_dir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private string Copy(string source, string name)
    {
        var target = Path.Combine(_dir, name);
        File.Copy(source, target, overwrite: true);
        return target;
    }

    private static CompiledMetadataReference Recorded(string path) =>
        new(Path.GetFileName(path), CompiledMetadataReferences.TryReadMvid(path)!.Value, true, false);
}
