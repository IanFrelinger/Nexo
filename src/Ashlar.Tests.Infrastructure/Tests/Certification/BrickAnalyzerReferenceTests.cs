using FluentAssertions;
using Ashlar.Infrastructure.Certification;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The second half of the analyzer-fence defect. The Ashlar.Analyzers package shipped its assembly
/// only under <c>lib/</c>, so a PackageReference loaded no analyzers and the fence — one of the
/// gate's five legs — silently did nothing for every external brick author. Worse, adding the
/// reference anyway made the brick UNCERTIFIABLE, because the dependency leg allowed exactly two
/// package names. The one leg an author could run locally was the one the rules refused.
///
/// <para>A reference that DOES reach the runtime graph is a third dependency however it is
/// labelled, and is still refused — with the exact attribute to add.</para>
///
/// <para>WHICH attribute is the part the first fix got wrong, and the part these facts now pin.
/// It accepted a bare <c>PrivateAssets="all"</c> on the reasoning that analyzers "put nothing into
/// the built brick". That is false for THIS package: <c>Ashlar.Analyzers</c> deliberately ships a
/// <c>lib/</c> leg beside <c>analyzers/dotnet/cs/</c>, and <c>PrivateAssets</c> only stops assets
/// flowing on to the referencing project's own consumers — the referencing project still receives
/// the compile and runtime assets. So the exemption let the analyzer DLL land in the brick's
/// output, let the brick bind to analyzer types and certify anyway, and produced a packed brick
/// declaring no such dependency: <c>FileNotFoundException</c> in a consumer's process. The shape
/// that is actually build-time-only is <c>ExcludeAssets="runtime;compile"</c>, which leaves the
/// <c>analyzers</c> asset group alone so the rules still RUN.</para>
/// </summary>
public sealed class BrickAnalyzerReferenceTests : IDisposable
{
    private readonly string _dir;

    public BrickAnalyzerReferenceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "brick-dep-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private string WriteProject(string packageReferences)
    {
        var path = Path.Combine(_dir, "Brick.csproj");
        File.WriteAllText(path, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.1" />
            {packageReferences}
              </ItemGroup>
            </Project>
            """);
        return path;
    }

    private const string Source = "public sealed class DemoBrick { }";

    [Fact]
    public void A_build_time_only_analyzer_reference_is_allowed()
    {
        var project = WriteProject(
            """    <PackageReference Include="Ashlar.Analyzers" Version="0.1.1" ExcludeAssets="runtime;compile" />""");

        var result = BrickDependencyChecker.Check(project, Source);

        result.Passed.Should().BeTrue(string.Join(" | ", result.Violations));
    }

    [Fact]
    public void The_element_form_of_ExcludeAssets_is_understood_too()
    {
        // MSBuild accepts metadata as an attribute or a child element. A checker that understood
        // only one would refuse a correctly-written project, which is its own silent trap.
        var project = WriteProject("""
                <PackageReference Include="Ashlar.Analyzers" Version="0.1.1">
                  <ExcludeAssets>runtime;compile</ExcludeAssets>
                </PackageReference>
            """);

        BrickDependencyChecker.Check(project, Source).Passed.Should().BeTrue();
    }

    [Fact]
    public void ExcludeAssets_all_counts_too_even_though_it_switches_the_rules_off()
    {
        // Nothing from the package reaches the brick, which is all the two-package rule is about.
        // It is a worse choice for the author, not a violation.
        var project = WriteProject(
            """    <PackageReference Include="Ashlar.Analyzers" Version="0.1.1" ExcludeAssets="all" />""");

        BrickDependencyChecker.Check(project, Source).Passed.Should().BeTrue();
    }

    [Fact]
    public void PrivateAssets_all_on_its_own_is_refused_because_the_assembly_still_lands_in_the_brick()
    {
        // The regression: this shape is what docs/CertificationGate.md told authors to copy, and
        // the checker accepted it. PrivateAssets governs TRANSITIVE flow; the referencing project
        // still gets the compile and runtime assets, and Ashlar.Analyzers ships a lib/ leg. So the
        // brick could reference analyzer types, certify clean, pack without declaring the
        // dependency, and die with FileNotFoundException in a consumer's process.
        var project = WriteProject("""    <PackageReference Include="Ashlar.Analyzers" Version="0.1.1" PrivateAssets="all" />""");

        var result = BrickDependencyChecker.Check(project, Source);

        result.Passed.Should().BeFalse(
            "a reference that still contributes runtime and compile assets is a third dependency, "
            + "whatever it is labelled");
        var violation = result.Violations.Should().ContainSingle().Subject;
        violation.Should().Contain("ExcludeAssets=\"runtime;compile\"", "the refusal names the exact edit");
        violation.Should().Contain("PrivateAssets", "and says why the obvious attribute is not the one");
    }

    [Fact]
    public void An_analyzer_reference_that_flows_to_the_runtime_graph_is_refused_with_the_edit()
    {
        var project = WriteProject("""    <PackageReference Include="Ashlar.Analyzers" Version="0.1.1" />""");

        var result = BrickDependencyChecker.Check(project, Source);

        result.Passed.Should().BeFalse();
        result.Violations.Should().ContainSingle()
            .Which.Should().Contain("ExcludeAssets=\"runtime;compile\"", "the refusal names the exact attribute to add");
    }

    [Fact]
    public void Excluding_only_runtime_is_not_enough_and_excluding_only_compile_is_not_either()
    {
        // Half the fix is not the fix: compile-only leaves the DLL in the output, runtime-only
        // leaves the brick able to bind to analyzer types.
        foreach (var half in new[] { "runtime", "compile" })
        {
            var project = WriteProject(
                $"""    <PackageReference Include="Ashlar.Analyzers" Version="0.1.1" ExcludeAssets="{half}" />""");

            BrickDependencyChecker.Check(project, Source).Passed.Should().BeFalse(
                $"ExcludeAssets=\"{half}\" leaves the other asset group flowing into the brick");
        }
    }

    [Fact]
    public void The_docs_and_the_checker_agree_on_the_shape_they_teach()
    {
        // The refusal text, the csproj comment and docs/CertificationGate.md all used to assert
        // that analyzers "put nothing into the built brick". They do, unless the reference is
        // shaped to stop them — so the one shape the docs show has to be the one the gate accepts.
        var docs = Path.Combine(RepoRoot(), "docs", "CertificationGate.md");
        File.Exists(docs).Should().BeTrue(docs);
        var text = File.ReadAllText(docs);

        // The version shown is whichever line the page documents (0.1.2 is the first with an
        // analyzers leg); the SHAPE is the invariant.
        text.Should().MatchRegex("""<PackageReference Include="Ashlar\.Analyzers" Version="\d+\.\d+\.\d+" ExcludeAssets="runtime;compile" />""");
        text.Should().NotMatchRegex("""<PackageReference Include="Ashlar\.Analyzers" Version="\d+\.\d+\.\d+" PrivateAssets="all" />""",
            "showing authors a shape the gate refuses is how the fix became the defect");
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Ashlar.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Ashlar.sln not found above " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Any_other_package_is_still_refused()
    {
        // The two-package rule is not relaxed: only a named build-time-only package is exempt.
        var project = WriteProject("""    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" PrivateAssets="all" />""");

        var result = BrickDependencyChecker.Check(project, Source);

        result.Passed.Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Should().Contain("Newtonsoft.Json");
    }

    [Fact]
    public void The_two_allowed_packages_still_pass_on_their_own()
    {
        var project = WriteProject("""    <PackageReference Include="Ashlar.Authoring" Version="0.1.1" />""");

        BrickDependencyChecker.Check(project, Source).Passed.Should().BeTrue();
    }
}
