using FluentAssertions;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks.Ports;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Port (a): GxxCompileValidator — fake ISandboxedCommandRunner with canned g++ logs.
/// </summary>
public sealed class GxxCompileValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ReturnsOk_WhenRunnerSucceeds()
    {
        var poco = CreateMinimalPocoContext();
        try
        {
            var runner = new FakeSandboxRunner((_, _) =>
                Task.FromResult(new ProcessCommandResult(0, "ok", "")));
            var validator = new GxxCompileValidator(
                runner,
                new FileScratchSpaceStub(),
                new GxxCompileValidatorOptions
                {
                    PocoContext = poco,
                    RequireSuccess = true,
                    Image = "dep-extract:latest"
                });

            var (ok, reason) = await validator.ValidateAsync(
                new GeneratedArtifact { Content = "// adapter\n" },
                CancellationToken.None);

            ok.Should().BeTrue();
            reason.Should().BeNull();
            runner.LastSpec.Should().NotBeNull();
            runner.LastSpec!.Network.Should().Be(NetworkAccess.None);
            runner.LastSpec.Entrypoint.Should().Be("g++");
            runner.LastSpec.Image.Should().Be("dep-extract:latest");
            runner.LastSpec.Mounts.Should().Contain(m => m.ContainerPath == "/common" && m.ReadOnly);
            runner.LastSpec.Mounts.Should().Contain(m => m.ContainerPath == "/work" && !m.ReadOnly);
            runner.LastSpec.Command.Should().Contain("-std=c++20");
            runner.LastSpec.Command.Should().Contain("/work/smoke.cpp");
        }
        finally
        {
            TryDelete(poco);
        }
    }

    [Fact]
    public async Task ValidateAsync_ReturnsGxxLogAsReason_WhenCompileFails()
    {
        var poco = CreateMinimalPocoContext();
        try
        {
            const string log = "error: 'CustomEventReader' was not declared";
            var runner = new FakeSandboxRunner((_, _) =>
                Task.FromResult(new ProcessCommandResult(1, "", log)));
            var validator = new GxxCompileValidator(
                runner,
                new FileScratchSpaceStub(),
                new GxxCompileValidatorOptions { PocoContext = poco, RequireSuccess = true });

            var (ok, reason) = await validator.ValidateAsync(
                new GeneratedArtifact { Content = "bad" },
                CancellationToken.None);

            ok.Should().BeFalse();
            reason.Should().Contain(log);
        }
        finally
        {
            TryDelete(poco);
        }
    }

    [Fact]
    public async Task ValidateAsync_SoftPasses_WhenPocoMissing_AndRequireSuccessFalse()
    {
        var runner = new FakeSandboxRunner((_, _) =>
            throw new InvalidOperationException("runner must not be called"));
        var validator = new GxxCompileValidator(
            runner,
            new FileScratchSpaceStub(),
            new GxxCompileValidatorOptions
            {
                PocoContext = Path.Combine(Path.GetTempPath(), "no-such-poco-" + Guid.NewGuid().ToString("n")),
                RequireSuccess = false
            });

        var (ok, reason) = await validator.ValidateAsync(
            new GeneratedArtifact { Content = "x" },
            CancellationToken.None);

        ok.Should().BeTrue();
        reason.Should().BeNull();
        runner.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ValidateAsync_HardFails_WhenPocoMissing_AndRequireSuccessTrue()
    {
        var validator = new GxxCompileValidator(
            new FakeSandboxRunner((_, _) => Task.FromResult(new ProcessCommandResult(0, "", ""))),
            new FileScratchSpaceStub(),
            new GxxCompileValidatorOptions
            {
                PocoContext = Path.Combine(Path.GetTempPath(), "no-such-poco-" + Guid.NewGuid().ToString("n")),
                RequireSuccess = true
            });

        var (ok, reason) = await validator.ValidateAsync(
            new GeneratedArtifact { Content = "x" },
            CancellationToken.None);

        ok.Should().BeFalse();
        reason.Should().Contain("processing.hpp");
    }

    [Fact]
    public async Task ValidateAsync_WritesCompanions_AndMountsParserAndShims()
    {
        var poco = CreateMinimalPocoContext();
        var parser = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "parser-" + Guid.NewGuid().ToString("n"))).FullName;
        var shims = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "shims-" + Guid.NewGuid().ToString("n"))).FullName;
        File.WriteAllText(Path.Combine(parser, "Foo.hpp"), "#pragma once\n");
        try
        {
            var sawAdapted = false;
            var sawSmoke = false;
            var sawHelper = false;
            var runner = new FakeSandboxRunner((spec, _) =>
            {
                var work = spec.Mounts.First(m => m.ContainerPath == "/work").HostPath;
                sawAdapted = File.Exists(Path.Combine(work, "adapted_reader.hpp"));
                sawSmoke = File.Exists(Path.Combine(work, "smoke.cpp"));
                sawHelper = File.Exists(Path.Combine(work, "helper.cpp"));
                return Task.FromResult(new ProcessCommandResult(0, "", ""));
            });
            var validator = new GxxCompileValidator(
                runner,
                new FileScratchSpaceStub(),
                new GxxCompileValidatorOptions
                {
                    PocoContext = poco,
                    ParserDir = parser,
                    ShimDir = shims,
                    RequireSuccess = true
                });

            await validator.ValidateAsync(
                new GeneratedArtifact
                {
                    Content = "#pragma once\n",
                    Files = new Dictionary<string, string> { ["helper.cpp"] = "int x(){return 0;}" }
                },
                CancellationToken.None);

            runner.LastSpec!.Mounts.Should().Contain(m => m.ContainerPath == "/parser" && m.ReadOnly);
            runner.LastSpec.Mounts.Should().Contain(m => m.ContainerPath == "/shims" && m.ReadOnly);
            runner.LastSpec.Command.Should().Contain(c => c.Contains("helper.cpp", StringComparison.Ordinal));
            sawAdapted.Should().BeTrue();
            sawSmoke.Should().BeTrue();
            sawHelper.Should().BeTrue();
        }
        finally
        {
            TryDelete(poco);
            TryDelete(parser);
            TryDelete(shims);
        }
    }

    private static string CreateMinimalPocoContext()
    {
        var root = Path.Combine(Path.GetTempPath(), "poco-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(root, "common"));
        File.WriteAllText(Path.Combine(root, "common", "processing.hpp"), "#pragma once\n");
        return root;
    }

    private static void TryDelete(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch { /* best-effort */ }
    }

    private sealed class FakeSandboxRunner : ISandboxedCommandRunner
    {
        private readonly Func<SandboxSpec, CancellationToken, Task<ProcessCommandResult>> _handler;
        public int Calls { get; private set; }
        public SandboxSpec? LastSpec { get; private set; }
        public string? LastWorkDir { get; private set; }

        public FakeSandboxRunner(Func<SandboxSpec, CancellationToken, Task<ProcessCommandResult>> handler) =>
            _handler = handler;

        public Task<ProcessCommandResult> RunAsync(SandboxSpec spec, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastSpec = spec;
            LastWorkDir = spec.Mounts.FirstOrDefault(m => m.ContainerPath == "/work")?.HostPath;
            return _handler(spec, cancellationToken);
        }
    }

    private sealed class FileScratchSpaceStub : IScratchSpace
    {
        public IScratchDir CreateScratchDir(string prefix)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():n}");
            Directory.CreateDirectory(path);
            return new Dir(path);
        }

        private sealed class Dir : IScratchDir
        {
            public Dir(string path) => Path = path;
            public string Path { get; }
            public void Dispose()
            {
                try { Directory.Delete(Path, recursive: true); } catch { /* ignore */ }
            }
        }
    }
}
