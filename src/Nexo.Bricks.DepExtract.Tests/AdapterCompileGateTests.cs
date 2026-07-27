using FluentAssertions;
using Nexo.Bricks.DepExtract;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class AdapterCompileGateTests
{
    static string? ResolvePoco()
    {
        var env = Environment.GetEnvironmentVariable("POCO_CONTEXT");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(Path.Combine(env, "common", "processing.hpp")))
            return Path.GetFullPath(env);
        var sibling = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Poco"));
        if (File.Exists(Path.Combine(sibling, "common", "processing.hpp")))
            return sibling;
        var home = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "work", "poco");
        if (File.Exists(Path.Combine(home, "common", "processing.hpp")))
            return home;
        return null;
    }

    static bool DockerAndImageReady()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "image", "inspect", "dep-extract:latest" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit(20_000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void Smoke_cpp_constructs_CustomEventReader()
    {
        var smoke = AdapterCompileGate.BuildSmokeCpp();
        smoke.Should().Contain("CustomEventReader");
        smoke.Should().Contain("next(");
    }

    [Fact]
    public async Task Compile_scaffold_against_P01_when_docker_poco_available()
    {
        var poco = ResolvePoco();
        if (poco is null || !DockerAndImageReady())
        {
            // Soft skip — CI without Poco/docker still runs other unit tests.
            return;
        }

        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "e2e", "projects", "P01_SimpleTickPull");
        var src = File.ReadAllText(Path.Combine(fixture, "SimpleTickStream.hpp"));
        var code = AdapterScaffold.TryBuild(src, "SimpleTickStream.hpp");
        code.Should().NotBeNullOrWhiteSpace();

        var result = await AdapterCompileGate.TryCompileAsync(
            code!, poco, fixture, requireSuccess: true);

        result.Skipped.Should().BeFalse();
        result.Ok.Should().BeTrue(result.Log);
    }

    [Fact]
    public async Task Compile_P10_companion_cpp_required()
    {
        var poco = ResolvePoco();
        if (poco is null || !DockerAndImageReady())
            return;

        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "e2e", "projects", "P10_NeedsCompanion");
        var src = File.ReadAllText(Path.Combine(fixture, "StreamWithCpp.hpp"));
        var code = AdapterScaffold.TryBuild(src, "StreamWithCpp.hpp");
        code.Should().NotBeNullOrWhiteSpace();

        var withCompanion = await AdapterCompileGate.TryCompileAsync(
            code!, poco, fixture, requireSuccess: true);
        withCompanion.Ok.Should().BeTrue(withCompanion.Log);

        // Without parserDir, companion .cpp is missing → link must fail.
        var without = await AdapterCompileGate.TryCompileAsync(
            code!, poco, parserDir: null, requireSuccess: true);
        without.Ok.Should().BeFalse();
    }
}
