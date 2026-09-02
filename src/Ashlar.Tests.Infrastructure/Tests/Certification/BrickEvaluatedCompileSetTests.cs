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
public sealed class BrickEvaluatedCompileSetTests : IDisposable
{
    private readonly string _dir;

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

    private string Csproj => Path.Combine(_dir, "Brick.csproj");

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

    // ── bypass 5 · the same hole one step later: a target, not an ItemGroup ────────────────

    [Fact]
    public async Task Bypass5_a_compile_item_added_by_a_target_is_refused_before_the_assembly_is_loaded()
    {
        // Asking MSBuild what a project EVALUATES to closes four bypasses and leaves this one: a
        // <Target BeforeTargets="CoreCompile"> adding a Compile item contributes nothing at
        // evaluation time and everything at compile time. Reproduced live on this repo — the
        // payload's type was in the built assembly and in none of the evaluation-time answers.
        //
        // The refusal must land BEFORE Assembly.LoadFrom, which executes the candidate's module
        // initializers inside this process.
        Write("Brick.cs", "public sealed class DemoBrick { }");
        Write("Payload.cstxt", "public static class Payload { public static int Answer() => 42; }");
        Write("witness.json", """{"brickId":"demo","cases":[]}""");
        WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <Target Name="Sneak" BeforeTargets="CoreCompile">
                <ItemGroup><Compile Include="Payload.cstxt" /></ItemGroup>
              </Target>
            </Project>
            """);

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Brick project refused*")
            .And.Message.Should().Contain("Payload.cstxt")
            .And.Contain("not in the set the certificate hashes");
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
}
