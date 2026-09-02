using FluentAssertions;
using Ashlar.CLI.Commands;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// The defect: <c>ashlar doctor</c> returned overall FAIL and exit 1 PERMANENTLY when the CLI was
/// installed as a dotnet tool — the supported external path — because its CLI smoke check shells
/// out to <c>dotnet run --project application/src/Ashlar.CLI</c> and there is no checkout to build.
/// The first diagnostic command a new user runs told them their installation was broken when it
/// was fine.
///
/// <para>The fix turns on one question — <em>is there a repo checkout here?</em> — so that is what
/// these pin. The applicability decision is a pure function of the directories searched, precisely
/// so it can be tested from a test host that is itself running inside the checkout.</para>
/// </summary>
public sealed class DoctorRepoLaneApplicabilityTests : IDisposable
{
    private readonly string _dir;

    public DoctorRepoLaneApplicabilityTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-doctor-lane-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void Outside_a_checkout_there_is_no_cli_project_to_build()
    {
        var elsewhere = Path.Combine(_dir, "tool-install");
        var cwd = Path.Combine(_dir, "somebodys-project");
        Directory.CreateDirectory(elsewhere);
        Directory.CreateDirectory(cwd);

        DoctorCommand.FindRepoCliProject(cwd, elsewhere).Should().BeNull(
            "this is the dotnet-tool install: neither the working directory nor the binary's own "
            + "directory sits under a checkout, so the repo lane cannot apply");
    }

    [Fact]
    public void Inside_a_checkout_the_project_is_found_from_a_subdirectory()
    {
        var repo = Path.Combine(_dir, "repo");
        var project = Path.Combine(repo, "application", "src", "Ashlar.CLI");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "Ashlar.CLI.csproj"), "<Project />");
        var deepInside = Path.Combine(repo, "a", "b", "c");
        Directory.CreateDirectory(deepInside);

        DoctorCommand.FindRepoCliProject(deepInside)
            .Should().Be(Path.Combine(project, "Ashlar.CLI.csproj"),
                "a developer running `ashlar` from a subdirectory of the repo is still in a checkout");
    }

    [Fact]
    public void A_build_output_location_inside_the_checkout_also_counts()
    {
        // The second search root: the running assembly. Someone can run the built CLI from a
        // temp working directory while the binary still lives inside the checkout.
        var repo = Path.Combine(_dir, "repo2");
        var project = Path.Combine(repo, "application", "src", "Ashlar.CLI");
        var binDir = Path.Combine(project, "bin", "Debug", "net10.0");
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(project, "Ashlar.CLI.csproj"), "<Project />");
        var unrelatedCwd = Path.Combine(_dir, "unrelated");
        Directory.CreateDirectory(unrelatedCwd);

        DoctorCommand.FindRepoCliProject(unrelatedCwd, binDir).Should().NotBeNull();
    }
}
