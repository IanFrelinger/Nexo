using FluentAssertions;
using Ashlar.Infrastructure.Certification;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The four certification bypasses that shared ONE root cause: the gate decided WHICH FILES it was
/// certifying by globbing <c>*.cs</c> and reading the <c>.csproj</c> as XML, instead of asking
/// MSBuild what the project compiles.
///
/// <para>Each of these reproduces live. (1) <c>&lt;Compile Include="Payload.cstxt" /&gt;</c> — csc
/// compiles any file it is handed, and the glob only sees <c>*.cs</c>; two bricks with different
/// behaviour got IDENTICAL content hashes and both ADMITTED. (2) <c>Sub/obj/Payload.cs</c> — the
/// SDK excludes the project's own <c>obj/</c> and <c>bin/</c>, while the gate excluded a directory
/// of that NAME at any depth; pure layout, no csproj edit, so it survived every csproj-level
/// refusal. (3) A <c>Directory.Build.props</c> beside the csproj — the loader and the dependency
/// checker each parsed ONE file while MSBuild evaluates the whole chain, so a ProjectReference and
/// a third-party package declared there were admitted and signed. (4) <c>&lt;Compile
/// Remove="Brick.cs" /&gt;&lt;Compile Include="Real.cstxt" /&gt;</c> — the hashed set became
/// DISJOINT from the compiled set, so the certificate was signed over a decoy the assembly does not
/// contain.</para>
///
/// <para>All four are one fact: an approximation of the compiler is not the compiler, and every gap
/// between them is a file that ships inside a signed assembly while no leg of the gate ever judged
/// it. These tests pin the answer coming from MSBuild's evaluated item lists, and pin the rule that
/// anything unresolvable is a REFUSAL rather than an omission.</para>
/// </summary>
[Trait("Category", "Certification")]
[Trait("Category", "SlowTier")]
public sealed class BrickEvaluatedCompileSetTests : IDisposable
{
    private readonly string _dir;

    /// <summary>
    /// A project name unique to this test, because the assembly takes its name from the project and
    /// the tests that run all the way to <c>Assembly.LoadFrom</c> share one process: a second
    /// "Brick.dll" fails with "assembly with same name is already loaded", which looks exactly like
    /// a gate refusal and is not one.
    /// </summary>
    private readonly string _projectName = "Brick" + Guid.NewGuid().ToString("N")[..8];

    public BrickEvaluatedCompileSetTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-evaluated-set-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private string Csproj => Path.Combine(_dir, _projectName + ".csproj");

    private void WriteProject(string xml) => File.WriteAllText(Csproj, xml);

    private void Write(string relativePath, string content)
    {
        var full = Path.Combine(_dir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private IEnumerable<string> SourceSetNames() =>
        BrickCertificationProjectLoader.FindBrickSourceFiles(_dir)
            .Select(f => Path.GetRelativePath(_dir, f).Replace(Path.DirectorySeparatorChar, '/'));

    // ── bypass 1 · a compiled file the *.cs glob cannot see ────────────────────────────────

    [Fact]
    public void Bypass1_a_compile_item_that_is_not_dot_cs_is_still_part_of_the_certified_set()
    {
        // csc compiles whatever the project hands it; ".cs" is decoration, not a rule. The glob's
        // filter was therefore a filter on the CERTIFIED set only — the compiled set kept the file.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("Payload.cstxt", "public static class Payload { public static int Answer() => 42; }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup><Compile Include="Payload.cstxt" /></ItemGroup>
            </Project>
            """);

        SourceSetNames().Should().BeEquivalentTo(["Brick.cs", "Payload.cstxt"],
            "the certified set is what MSBuild hands the compiler, and it hands it Payload.cstxt");
    }

    [Fact]
    public async Task Bypass1_the_extra_compiled_file_no_longer_slips_the_multi_file_refusal()
    {
        // The end of the bypass: two bricks differing only in Payload.cstxt used to certify Trusted
        // under the same contentHash, because the second file was invisible to the file count too.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("Payload.cstxt", "public static class Payload { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup><Compile Include="Payload.cstxt" /></ItemGroup>
            </Project>
            """);

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Multi-file brick refused*")
            .And.Message.Should().Contain("Payload.cstxt");
    }

    // ── bypass 2 · obj/ and bin/ at a depth the SDK does not exclude ───────────────────────

    [Fact]
    public void Bypass2_a_nested_obj_directory_does_not_hide_compiled_source()
    {
        // $(BaseIntermediateOutputPath) is the project's OWN obj/. A directory called obj two
        // levels down is ordinary project source to the SDK — and was invisible to the gate, with
        // no csproj edit for any csproj-level refusal to catch.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("Sub/obj/Payload.cs", "public static class Payload { }");
        Write("Sub/bin/Other.cs", "public static class Other { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        SourceSetNames().Should().BeEquivalentTo(["Brick.cs", "Sub/obj/Payload.cs", "Sub/bin/Other.cs"]);
    }

    [Fact]
    public void Bypass2_the_projects_own_build_output_is_still_excluded()
    {
        // The fix must not swing the other way and start hashing the build's own output. MSBuild's
        // answer excludes exactly the two the SDK excludes, which is the point of asking it.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("obj/Debug/Brick.AssemblyInfo.cs", "// generated");
        Write("bin/Debug/Leftover.cs", "// stale output");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        SourceSetNames().Should().BeEquivalentTo(["Brick.cs"]);
    }

    // ── bypass 3 · the import chain the XML scan never read ────────────────────────────────

    [Fact]
    public void Bypass3_a_project_reference_declared_in_directory_build_props_is_refused()
    {
        // Verified live before the fix: these exact items in the csproj were correctly refused, and
        // the identical items in a Directory.Build.props beside it were ADMITTED and signed, with
        // both DLLs present in the built output.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        Write("Directory.Build.props", """
            <Project>
              <ItemGroup>
                <ProjectReference Include="..\Smuggled\Smuggled.csproj" />
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """);

        var result = BrickDependencyChecker.Check(Csproj, "public sealed class DemoBrick { }");

        result.Passed.Should().BeFalse(
            "where an item is declared is not a fact about the brick; what the brick depends on is");
        result.Violations.Should().Contain(v => v.Contains("ProjectReference forbidden", StringComparison.Ordinal));
        result.Violations.Should().Contain(v => v.Contains("Newtonsoft.Json", StringComparison.Ordinal));
        result.Violations.Should().Contain(v => v.Contains("Directory.Build.props", StringComparison.Ordinal),
            "a refusal naming a reference the author's csproj does not contain must say where it came from");
    }

    [Fact]
    public void Bypass3_a_compile_item_added_by_directory_build_props_from_outside_is_refused()
    {
        // The loader leg of the same hole: the outside-the-directory refusal read only the csproj,
        // so the props file could add the compile item the csproj was refused for.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        Write("Directory.Build.props", """
            <Project>
              <ItemGroup><Compile Include="..\Shared\Helper.cs" /></ItemGroup>
            </Project>
            """);

        var act = () => BrickCertificationProjectLoader.FindBrickSourceFiles(_dir);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("outside the brick directory").And.Contain("Fix:");
    }

    // ── bypass 4 · Remove, which the code called fail-closed and is not ────────────────────

    [Fact]
    public void Bypass4_a_compile_remove_leaves_the_hashed_set_equal_to_the_compiled_set()
    {
        // The comment that licensed this said a Remove "can only make the set a SUPERSET, which is
        // fail-closed". It makes the set DISJOINT: the gate hashed, analyzed and mutated Brick.cs,
        // and the assembly contained Real.cstxt. The certificate was signed over a decoy.
        Write("Brick.cs", "public sealed class Decoy { }");
        Write("Real.cstxt", "public sealed class Real { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup>
                <Compile Remove="Brick.cs" />
                <Compile Include="Real.cstxt" />
              </ItemGroup>
            </Project>
            """);

        SourceSetNames().Should().BeEquivalentTo(["Real.cstxt"],
            "the removed file is not compiled and the included one is; hashing the reverse signs a decoy");
    }

    // ── bypasses 5-7 · the same hole after evaluation: targets, not ItemGroups ─────────────
    //
    // These three are one family, and they are why the gate compares against the COMPILER's record
    // (the PDB source-document table) rather than against anything MSBuild reports. Each was
    // reproduced live on this repo, with the payload's type present in the built assembly:
    //
    //   5. A <Target BeforeTargets="CoreCompile"> adding a Compile item contributes nothing at
    //      evaluation time and everything at compile time.
    //   6. Give that target a Condition on $(OutputPath) and it also contributes nothing to a
    //      SECOND MSBuild query — the gate builds into a temp directory and a separate query does
    //      not, so the two answers necessarily differ. "Ask again after the build" splits here.
    //   7. Ask in the build's own invocation and a second target, AfterTargets="CoreCompile",
    //      removes the payload from @(Compile) once the compile has happened. The item list is
    //      clean; the assembly is not.
    //
    // @(Compile) is mutable state belonging to the project under judgement. The compiler's record
    // is not.

    [Fact]
    public Task Bypass5_a_compile_item_added_by_a_target_is_refused_before_the_assembly_is_loaded() =>
        AssertPayloadRefused("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <Target Name="Sneak" BeforeTargets="CoreCompile">
                <ItemGroup><Compile Include="Payload.cstxt" /></ItemGroup>
              </Target>
            </Project>
            """);

    [Fact]
    public Task Bypass6_a_target_conditioned_on_a_property_the_gate_cannot_match_is_refused() =>
        // $(OutputPath) is the gate's own temp build directory and nothing else's. A target
        // conditioned on it runs during certification and lies dormant in any second query, which
        // is what makes "verify with another MSBuild invocation" unsound in principle rather than
        // just incomplete.
        AssertPayloadRefused("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <Target Name="Sneak" BeforeTargets="CoreCompile"
                      Condition="$(OutputPath.Contains('ashlar-cert-build'))">
                <ItemGroup><Compile Include="Payload.cstxt" /></ItemGroup>
              </Target>
            </Project>
            """);

    [Fact]
    public Task Bypass7_a_target_that_scrubs_the_item_after_the_compile_is_refused() =>
        // The compile has already happened when Cover runs, so every reading of @(Compile) after
        // the build — including one taken from the build's own invocation — reports a clean
        // two-file project while Payload's type sits in the assembly.
        AssertPayloadRefused("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <Target Name="Sneak" BeforeTargets="CoreCompile">
                <ItemGroup><Compile Include="Payload.cstxt" /></ItemGroup>
              </Target>
              <Target Name="Cover" AfterTargets="CoreCompile">
                <ItemGroup><Compile Remove="Payload.cstxt" /></ItemGroup>
              </Target>
            </Project>
            """);

    [Fact]
    public Task Bypass7_the_scrub_does_not_work_by_dropping_the_debug_record_either() =>
        // The obvious next move once the PDB is the authority: switch it off. It is forced on as a
        // GLOBAL property, which the project under judgement cannot override — from its csproj,
        // from a Directory.Build.props, or from a PropertyGroup inside one of its own targets.
        AssertPayloadRefused("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <DebugType>none</DebugType>
              </PropertyGroup>
              <Target Name="Sneak" BeforeTargets="CoreCompile">
                <ItemGroup><Compile Include="Payload.cstxt" /></ItemGroup>
              </Target>
              <Target Name="Cover" AfterTargets="CoreCompile">
                <ItemGroup><Compile Remove="Payload.cstxt" /></ItemGroup>
              </Target>
            </Project>
            """);

    [Fact]
    public async Task A_target_that_deletes_the_compilers_record_is_refused_rather_than_waved_through()
    {
        // DebugType is forced as a global property, so the project cannot switch the record off —
        // but a target can still delete the file after the build. "The gate cannot establish what
        // was compiled" must be a refusal in that case too, not an empty document set that trivially
        // agrees with whatever was hashed. Verified live: the output keeps p.dll and loses p.pdb.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("witness.json", """{"brickId":"demo","cases":[]}""");
        WriteProject($"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <Target Name="Nuke" AfterTargets="Build">
                <Delete Files="$(OutputPath){_projectName}.pdb" />
              </Target>
            </Project>
            """);

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("no portable debug information")
            .And.Contain("Fix:")
            .And.Contain("deletes the .pdb",
                "the fix must name what actually happened — the project cannot have set DebugType, "
                + "because the gate forces it");
    }

    [Fact]
    public async Task A_target_that_rewrites_the_brick_source_before_the_compile_is_refused()
    {
        // The path sets can agree perfectly and the certificate still be wrong: the gate reads and
        // hashes Brick.cs, then the build rewrites it and compiles something else. The compiler
        // records a checksum per file, so the bytes that were hashed must be the bytes that were
        // compiled — one file, one text, or a refusal.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("witness.json", """{"brickId":"demo","cases":[]}""");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <Target Name="Swap" BeforeTargets="CoreCompile">
                <WriteLinesToFile File="Brick.cs" Overwrite="true"
                                  Lines="public sealed class DemoBrick { public static int Answer() =&gt; 42%3B }" />
              </Target>
            </Project>
            """);

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("Brick.cs")
            .And.Contain("is not the text that was compiled")
            .And.Contain("Fix:");
    }

    /// <summary>
    /// Builds a brick whose project smuggles <c>Payload.cstxt</c> into the compilation past
    /// evaluation, and requires the gate to refuse it by name — before <c>Assembly.LoadFrom</c>,
    /// which executes the candidate's module initializers inside this process.
    /// </summary>
    private async Task AssertPayloadRefused(string projectXml)
    {
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("Payload.cstxt", "public static class Payload { public static int Answer() => 42; }");
        Write("witness.json", """{"brickId":"demo","cases":[]}""");
        WriteProject(projectXml);

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("Payload.cstxt")
            .And.Contain("not in the set the certificate hashes")
            .And.Contain("Fix:");
    }

    // ── the same root cause on the dependency leg: code and DLLs that are not Compile items ─

    [Fact]
    public void An_analyzer_item_is_refused_because_a_source_generator_writes_uncertified_code()
    {
        // A source generator IS an analyzer. Its output is compiled into the brick without ever
        // being a source file, so no amount of care about the compile set can see it — and the XML
        // scan looked only at PackageReference and ProjectReference.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup><Analyzer Include="Generator.dll" /></ItemGroup>
            </Project>
            """);

        var result = BrickDependencyChecker.Check(Csproj, "public sealed class DemoBrick { }");

        result.Passed.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("Analyzer forbidden", StringComparison.Ordinal)
            && v.Contains("Generator.dll", StringComparison.Ordinal));
    }

    [Fact]
    public void The_sdks_own_implicit_analyzers_are_not_held_against_the_brick()
    {
        // Every SDK project carries Microsoft.CodeAnalysis.NetAnalyzers implicitly. A rule that
        // refused those would refuse every brick, which is a new way to fail an honest author.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        BrickDependencyChecker.Check(Csproj, "public sealed class DemoBrick { }")
            .Passed.Should().BeTrue();
    }

    [Fact]
    public void A_raw_assembly_reference_is_refused_rather_than_walking_past_the_package_allow_list()
    {
        // <Reference Include="X"><HintPath/></Reference> puts a third-party DLL in the brick's
        // output and lets the brick bind to its types, while the packed brick declares no such
        // dependency — and it never passes through the PackageReference allow-list at all.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup>
                <Reference Include="Newtonsoft.Json"><HintPath>lib/Newtonsoft.Json.dll</HintPath></Reference>
              </ItemGroup>
            </Project>
            """);

        var result = BrickDependencyChecker.Check(Csproj, "public sealed class DemoBrick { }");

        result.Passed.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("Reference forbidden", StringComparison.Ordinal)
            && v.Contains("Newtonsoft.Json", StringComparison.Ordinal));
    }

    // ── the invariant: unresolvable is a refusal, never an omission ────────────────────────

    [Fact]
    public void A_compile_item_with_no_file_on_disk_is_refused_not_skipped()
    {
        // A source the build generates on its way past is compiled into the assembly and cannot be
        // hashed here. Skipping it would be the omission this whole area exists to prevent.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup><Compile Include="Generated.cs" /></ItemGroup>
            </Project>
            """);

        var act = () => BrickCertificationProjectLoader.FindBrickSourceFiles(_dir);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("Generated.cs").And.Contain("Fix:");
    }

    [Fact]
    public void A_project_msbuild_cannot_evaluate_is_refused()
    {
        // Broken XML, a broken import, a missing SDK: whatever the reason, "cannot establish the
        // compiled set" must never degrade to "assume the directory listing".
        Write("Brick.cs", "public sealed class DemoBrick { }");
        WriteProject("<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><Compile Include=\"x.cs\" ");

        var act = () => BrickCertificationProjectLoader.FindBrickSourceFiles(_dir);

        act.Should().Throw<InvalidOperationException>()
            .And.Message.Should().Contain("could not establish what").And.Contain("Fix:");
    }

    [Fact]
    public void A_multi_targeted_brick_is_refused_rather_than_certified_for_one_framework()
    {
        // One content hash cannot speak for a per-framework compiled set.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFrameworks>net8.0;net10.0</TargetFrameworks></PropertyGroup>
            </Project>
            """);

        var act = () => BrickCertificationProjectLoader.FindBrickSourceFiles(_dir);

        act.Should().Throw<InvalidOperationException>()
            .And.Message.Should().Contain("multi-targets").And.Contain("Fix:");
    }

    [Fact]
    public void An_ordinary_single_file_brick_is_unaffected()
    {
        // The strictness must cost nothing to the shape the CLI scaffold emits, or it is just a new
        // way to fail an author who did nothing wrong.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.1" />
                <PackageReference Include="Ashlar.Authoring" Version="0.1.1" />
              </ItemGroup>
            </Project>
            """);

        SourceSetNames().Should().BeEquivalentTo(["Brick.cs"]);
        BrickDependencyChecker.Check(Csproj, "public sealed class DemoBrick { }")
            .Passed.Should().BeTrue();
    }

    // ── the other half: the fix must not become a blanket refusal ──────────────────────────
    //
    // Every leg above refuses something. These are the honest shapes that must still get all the
    // way THROUGH the compiled-set comparison — a gate that refuses everything is not a strict
    // gate, it is a broken one, and the strictest legs here are the newest and least exercised.
    //
    // They assert on how far the load got rather than on success, because a brick that carries no
    // DomainBrick type cannot finish LoadAsync and one that does would need the contracts package
    // restored from the network. "No DomainBrick type in assembly" is raised AFTER the build, after
    // the compiled-set comparison and at Assembly.LoadFrom — so seeing it is proof that every check
    // this file adds let the project past.

    [Theory]
    [InlineData("an ordinary single-file brick", """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
        </Project>
        """)]
    [InlineData("a brick whose SDK generates assembly-info and global usings under obj/", """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
          </PropertyGroup>
        </Project>
        """)]
    [InlineData("a brick that relocates its own intermediate output", """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <BaseIntermediateOutputPath>build-temp/</BaseIntermediateOutputPath>
          </PropertyGroup>
        </Project>
        """)]
    [InlineData("a deterministic CI build", """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
            <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
            <Deterministic>true</Deterministic>
          </PropertyGroup>
        </Project>
        """)]
    [InlineData("the analyzer ExcludeAssets shape docs/CertificationGate.md teaches", """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Ashlar.Analyzers" Version="0.1.1" ExcludeAssets="runtime;compile" />
          </ItemGroup>
        </Project>
        """)]
    public async Task An_honest_brick_gets_past_the_compiled_set_comparison(string shape, string projectXml)
    {
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("witness.json", """{"brickId":"demo","cases":[]}""");
        WriteProject(projectXml);

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<InvalidOperationException>(shape))
            .And.Message.Should()
            .Be("No DomainBrick type in assembly",
                $"{shape} must reach Assembly.LoadFrom — every refusal before it would be a blanket refusal "
                + "of a project that did nothing wrong");
    }

    [Fact]
    public async Task Following_the_fix_the_payload_refusal_names_actually_clears_it()
    {
        // The refusals in this area are the part of the gate authors praise, and the way to ruin
        // that is to name a fix that leads into a DIFFERENT refusal. Bypass 5's message says:
        // remove the target, and if the code belongs in the brick move it into the brick's own
        // single source file. This walks exactly that, and requires the project to get all the way
        // through the compiled-set comparison afterwards — an earlier wording said "declare it as
        // an ordinary Compile item", which does clear THIS refusal and lands on the multi-file one.
        Write("Brick.cs", "public sealed class DemoBrick { public static int Answer() => 42; }");
        Write("witness.json", """{"brickId":"demo","cases":[]}""");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .And.Message.Should().Be("No DomainBrick type in assembly",
                "the fix the refusal names must leave the author past every check this file adds, not "
                + "at the next refusal along");
    }

    [Fact]
    public async Task A_source_generator_that_writes_code_into_the_brick_is_refused_by_name()
    {
        // The one shape that legitimately puts code in the assembly without a Compile item. It must
        // be refused rather than tolerated — generated code in a signed brick is code no leg of the
        // gate judged — and the refusal must say so rather than blaming the author's own file.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("witness.json", """{"brickId":"demo","cases":[]}""");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
              </PropertyGroup>
              <Target Name="Sneak" BeforeTargets="CoreCompile">
                <ItemGroup><Compile Include="obj/Generated.cs" /></ItemGroup>
              </Target>
              <Target Name="MakeIt" BeforeTargets="Sneak">
                <WriteLinesToFile File="obj/Generated.cs" Overwrite="true"
                                  Lines="public static class Generated { }" />
              </Target>
            </Project>
            """);

        // obj/ is the ONE place the gate tolerates an unhashed compiled file, so a payload dropped
        // there is the sharpest test of the rule: tolerance turns on MSBuild saying the SDK's own
        // files declared the item, not on the directory and not on the file's name.
        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("Generated.cs").And.Contain("Fix:");
    }
}
