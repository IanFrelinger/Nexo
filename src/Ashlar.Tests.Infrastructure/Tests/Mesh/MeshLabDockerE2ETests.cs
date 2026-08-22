using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Ashlar.Tests.Infrastructure.Tests.Mesh;

/// <summary>
/// Optional end-to-end checks against the Docker mesh virtual lab (same scripts as CI <c>mesh-lab-gate</c>).
/// Enable with <c>ASHLAR_RUN_MESH_LAB=1</c> (requires Docker, bash, python3). Default CI/dotnet runs report these
/// tests as Skipped (see <see cref="OptInFactAttribute"/>); the fixture stays a no-op until opted in.
/// </summary>
[Collection("MeshLabDocker")]
[Trait("Category", "DockerOptional")]
[Trait("Category", "MeshLab")]
public sealed class MeshLabDockerE2ETests
{
    private readonly MeshLabDockerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public MeshLabDockerE2ETests(MeshLabDockerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    /// <summary>Runs standard then deep verify scripts (same order as <c>mesh-lab-gate.yml</c>).</summary>
    [OptInFact("ASHLAR_RUN_MESH_LAB", "Docker mesh virtual lab (Docker, bash, python3)", Timeout = 900_000)]
    public async Task Mesh_lab_compose_gate_standard_and_deep_verify()
    {
        // Opted in but the lab could not come up (no Docker daemon, ports busy, ...): keep the legacy
        // soft return for the post-opt-in runtime case; the discovery-time gate is the attribute.
        if (!_fixture.IsReady)
        {
            _output.WriteLine(_fixture.SkipReason ?? "Mesh lab fixture not ready.");
            return;
        }

        await RunVerifyScriptAsync("scripts/mesh-lab-verify.sh");

        if (!MeshLabDockerEnv.RunDeep)
        {
            _output.WriteLine("Skipping deep mesh verify (ASHLAR_MESH_LAB_SKIP_DEEP=1).");
            return;
        }

        await RunVerifyScriptAsync("scripts/mesh-lab-verify-deep.sh");
    }

    private async Task RunVerifyScriptAsync(string relativeScriptPath)
    {
        var repoRoot = TestPaths.FindRepoRoot();
        var scriptPath = Path.Combine(repoRoot, relativeScriptPath);
        Assert.True(File.Exists(scriptPath), $"mesh lab script missing: {relativeScriptPath}");

        var env = new Dictionary<string, string?>(_fixture.VerifyScriptEnvironment);

        await MeshLabProcessRunner.AssertSuccessAsync(
            repoRoot,
            "bash",
            $"\"{scriptPath}\" \"{_fixture.EnvFilePath}\"",
            env,
            TimeSpan.FromMinutes(8));
    }
}
