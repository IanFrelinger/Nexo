using FluentAssertions;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Execution.Sandbox;
using Nexo.Infrastructure.Scaling;
using Xunit;

namespace Nexo.Tests.Application.Tests.Execution;

/// <summary>
/// Step 2: ISandboxedCommandRunner / SandboxSpec — Docker backend builds
/// isolation args from a domain-neutral spec; default network is none.
/// </summary>
public sealed class SandboxedCommandRunnerTests
{
    [Fact]
    public void SandboxSpec_Defaults_NetworkToNone()
    {
        var spec = new SandboxSpec(
            Image: "tool:latest",
            Mounts: Array.Empty<Mount>(),
            Network: NetworkAccess.None,
            Command: new[] { "echo", "hi" });

        spec.Network.Should().Be(NetworkAccess.None);
    }

    [Fact]
    public void BuildDockerArguments_IncludesNetworkNone_ByDefault()
    {
        var spec = new SandboxSpec(
            Image: "tool:latest",
            Mounts: new[] { new Mount("/host/src", "/src", ReadOnly: true) },
            Network: NetworkAccess.None,
            Command: new[] { "g++", "-c", "a.cpp" },
            Limits: new ResourceLimits(Memory: "2g", Pids: 256, Cpus: "2", Timeout: TimeSpan.FromSeconds(120)));

        var args = DockerSandboxedCommandRunner.BuildDockerArguments(spec);

        args.Should().ContainInOrder("run", "--rm");
        args.Should().Contain("--network=none");
        args.Should().Contain("--memory");
        args.Should().Contain("2g");
        args.Should().Contain("--cpus");
        args.Should().Contain("2");
        args.Should().Contain("--pids-limit");
        args.Should().Contain("256");
        args.Should().Contain("-v");
        args.Should().Contain("/host/src:/src:ro");
        args.Should().Contain("tool:latest");
        args.Should().ContainInOrder("g++", "-c", "a.cpp");
    }

    [Fact]
    public void BuildDockerArguments_AlwaysHardensTheContainer_RegardlessOfSpec()
    {
        // The hardening flags are not spec-optional: no capabilities, no privilege
        // escalation, no image fetch, sealed rootfs — every sandbox container, every time.
        var spec = new SandboxSpec(
            Image: "tool:latest",
            Mounts: Array.Empty<Mount>(),
            Network: NetworkAccess.Unrestricted,
            Command: new[] { "true" });

        var args = DockerSandboxedCommandRunner.BuildDockerArguments(spec);

        args.Should().ContainInOrder("--cap-drop", "ALL");
        args.Should().ContainInOrder("--security-opt", "no-new-privileges");
        args.Should().ContainInOrder("--pull", "never");
        args.Should().Contain("--read-only");
        args.Should().NotContain("--tmpfs", "a spec that declares no write surface gets no scratch");
        var ordered = args.ToList();
        ordered.IndexOf("--read-only").Should().BeLessThan(ordered.IndexOf("tool:latest"),
            "hardening flags are docker options and must precede the image");
    }

    [Fact]
    public void BuildDockerArguments_BacksEachScratchPath_WithACappedTmpfs()
    {
        var spec = new SandboxSpec(
            Image: "tool:latest",
            Mounts: Array.Empty<Mount>(),
            Network: NetworkAccess.None,
            Command: new[] { "make" })
        {
            ScratchPaths = new[] { "/work", "/tmp", " ", "C:\\odd\\path" },
        };

        var args = DockerSandboxedCommandRunner.BuildDockerArguments(spec);

        args.Should().Contain("--read-only");
        args.Should().ContainInOrder("--tmpfs", $"/work:{DockerSandboxedCommandRunner.ScratchPathMountOptions}");
        args.Should().ContainInOrder("--tmpfs", $"/tmp:{DockerSandboxedCommandRunner.ScratchPathMountOptions}");
        args.Should().Contain($"C:/odd/path:{DockerSandboxedCommandRunner.ScratchPathMountOptions}",
            "separators are normalized like mounts are");
        args.Count(a => a == "--tmpfs").Should().Be(3, "blank entries are skipped");
        DockerSandboxedCommandRunner.ScratchPathMountOptions.Should().Contain("size=",
            "scratch is capped, not unbounded host memory");
        DockerSandboxedCommandRunner.ScratchPathMountOptions.Should().Contain("nosuid");
    }

    [Fact]
    public void BuildDockerArguments_OmitsNetworkNone_WhenUnrestricted()
    {
        var spec = new SandboxSpec(
            Image: "tool:latest",
            Mounts: Array.Empty<Mount>(),
            Network: NetworkAccess.Unrestricted,
            Command: new[] { "true" });

        var args = DockerSandboxedCommandRunner.BuildDockerArguments(spec);

        args.Should().NotContain(a => a.StartsWith("--network", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildDockerArguments_IncludesEntrypoint_WhenSet()
    {
        var spec = new SandboxSpec(
            Image: "tool:latest",
            Mounts: Array.Empty<Mount>(),
            Network: NetworkAccess.None,
            Command: new[] { "-std=c++20", "a.cpp" },
            Entrypoint: "g++");

        var args = DockerSandboxedCommandRunner.BuildDockerArguments(spec);

        args.Should().ContainInOrder("--entrypoint", "g++", "tool:latest", "-std=c++20", "a.cpp");
    }

    [Fact]
    public void BuildDockerArguments_Throws_WhenImageMissing()
    {
        var spec = new SandboxSpec(
            Image: null,
            Mounts: Array.Empty<Mount>(),
            Network: NetworkAccess.None,
            Command: new[] { "true" });

        var act = () => DockerSandboxedCommandRunner.BuildDockerArguments(spec);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_DelegatesToProcessRunner_WithDockerCli()
    {
        IReadOnlyList<string>? capturedArgs = null;
        var fake = new FakeProcessRunner((file, args) =>
        {
            file.Should().Be("docker");
            capturedArgs = args.ToArray();
            return Task.FromResult(new ProcessCommandResult(0, "ok", ""));
        });

        var runner = new DockerSandboxedCommandRunner(fake);
        var result = await runner.RunAsync(
            new SandboxSpec(
                Image: "img:1",
                Mounts: Array.Empty<Mount>(),
                Network: NetworkAccess.None,
                Command: new[] { "echo", "x" },
                Limits: new ResourceLimits(Timeout: TimeSpan.FromSeconds(30))),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.StdOut.Should().Be("ok");
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Should().Contain("--network=none");
        capturedArgs.Should().Contain("img:1");
    }

    [Fact]
    public async Task RunAsync_PropagatesNonZeroExit()
    {
        var fake = new FakeProcessRunner((_, _) =>
            Task.FromResult(new ProcessCommandResult(1, "", "compile failed")));
        var runner = new DockerSandboxedCommandRunner(fake);

        var result = await runner.RunAsync(
            new SandboxSpec("img:1", Array.Empty<Mount>(), NetworkAccess.None, new[] { "false" }),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(1);
        result.StdErr.Should().Be("compile failed");
    }

    private sealed class FakeProcessRunner : IProcessCommandRunner
    {
        private readonly Func<string, IReadOnlyList<string>, Task<ProcessCommandResult>> _handler;

        public FakeProcessRunner(Func<string, IReadOnlyList<string>, Task<ProcessCommandResult>> handler) =>
            _handler = handler;

        public Task<ProcessCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default) =>
            _handler(fileName, arguments);
    }
}
