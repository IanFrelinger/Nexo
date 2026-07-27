using FluentAssertions;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Infrastructure.Execution.Scratch;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>Port (c): PocoInstallTarget over IManagedFileSet + ISingleFlightGuard.</summary>
public sealed class PocoInstallTargetTests
{
    [Fact]
    public async Task ApplyAsync_WritesAdapterThroughManagedFileSet_OnSuccess()
    {
        var poco = CreateMinimalPoco();
        var ops = new FakeOps();
        var target = CreateTarget(poco, ops, rebuild: true, recreate: true, smoke: false);

        var result = await target.ApplyAsync(new GeneratedArtifact
        {
            Content = "#pragma once\nclass AdaptedReader {};\n",
            Files = new Dictionary<string, string> { ["Helper.hpp"] = "// help\n" }
        });

        result.Succeeded.Should().BeTrue();
        File.Exists(Path.Combine(poco, "common", "adapted_reader.hpp")).Should().BeTrue();
        File.Exists(Path.Combine(poco, "common", "Helper.hpp")).Should().BeTrue();
        var body = await File.ReadAllTextAsync(Path.Combine(poco, "common", "adapted_reader.hpp"));
        body.Should().Contain("NEXO_ADAPTER_PROVENANCE_BEGIN");
        ops.BuildCalls.Should().Be(1);
        ops.RecreateCalls.Should().Be(1);
        ops.EnsureCalls.Should().Be(1);
        ops.Upserts.Should().Contain(("EVTX_IMAGE", "evtx:custom"));
    }

    [Fact]
    public async Task ApplyAsync_RollsBackManagedFiles_WhenBuildFails()
    {
        var poco = CreateMinimalPoco();
        var prior = Path.Combine(poco, "common", "adapted_reader.hpp");
        await File.WriteAllTextAsync(prior, "PRIOR_ADAPTER");
        await File.WriteAllTextAsync(Path.Combine(poco, "common", "Old.hpp"), "old-companion");
        await File.WriteAllTextAsync(
            Path.Combine(poco, "common", ".nexo-adapter-companions"),
            "Old.hpp");

        var ops = new FakeOps
        {
            BuildResult = (false, "g++: error: undefined reference to `foo'")
        };
        var files = new TrackingManagedFileSet(new SnapshotManagedFileSet());
        var target = new PocoInstallTarget(
            files,
            new SingleFlightGuard(),
            ops,
            new PocoInstallTargetOptions
            {
                PocoContext = poco,
                RebuildImage = true,
                RecreateEvtx = true,
                Smoke = false,
                Strategy = "scaffold"
            });

        var result = await target.ApplyAsync(new GeneratedArtifact
        {
            Content = "#pragma once\nclass AdaptedReader {};\n",
            Files = new Dictionary<string, string> { ["New.hpp"] = "new\n" }
        });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("docker build failed");
        files.RollbackCalls.Should().Be(1);
        (await File.ReadAllTextAsync(prior)).Should().Be("PRIOR_ADAPTER");
        File.Exists(Path.Combine(poco, "common", "New.hpp")).Should().BeFalse();
        File.Exists(Path.Combine(poco, "common", "Old.hpp")).Should().BeTrue();
        ops.RecreateCalls.Should().Be(0);
    }

    [Fact]
    public async Task ApplyAsync_RejectsDisallowedService()
    {
        var poco = CreateMinimalPoco();
        var ops = new FakeOps();
        var target = new PocoInstallTarget(
            new SnapshotManagedFileSet(),
            new SingleFlightGuard(),
            ops,
            new PocoInstallTargetOptions
            {
                PocoContext = poco,
                RebuildImage = true,
                RecreateEvtx = true,
                RecreateService = "not-allowed",
                Smoke = false
            });

        var result = await target.ApplyAsync(new GeneratedArtifact
        {
            Content = "#pragma once\nclass AdaptedReader {};\n"
        });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("not allowed");
        File.Exists(Path.Combine(poco, "common", "adapted_reader.hpp")).Should().BeFalse();
    }

    [Fact]
    public async Task ApplyAsync_SerializesWithSingleFlightGuard()
    {
        var poco = CreateMinimalPoco();
        var gate = new ManualResetEventSlim(false);
        var entered = 0;
        var max = 0;
        var ops = new FakeOps
        {
            OnBuild = async () =>
            {
                var now = Interlocked.Increment(ref entered);
                Interlocked.Exchange(ref max, Math.Max(max, now));
                gate.Wait(TimeSpan.FromSeconds(2));
                Interlocked.Decrement(ref entered);
                await Task.CompletedTask;
            }
        };
        var target = CreateTarget(poco, ops, rebuild: true, recreate: false, smoke: false);
        var artifact = new GeneratedArtifact { Content = "#pragma once\nclass AdaptedReader {};\n" };

        var t1 = Task.Run(() => target.ApplyAsync(artifact));
        var t2 = Task.Run(() => target.ApplyAsync(artifact));
        await Task.Delay(50);
        gate.Set();
        await Task.WhenAll(t1, t2);

        max.Should().Be(1);
        ops.BuildCalls.Should().Be(2);
    }

    private static PocoInstallTarget CreateTarget(
        string poco,
        FakeOps ops,
        bool rebuild,
        bool recreate,
        bool smoke) =>
        new(
            new SnapshotManagedFileSet(),
            new SingleFlightGuard(),
            ops,
            new PocoInstallTargetOptions
            {
                PocoContext = poco,
                RebuildImage = rebuild,
                RecreateEvtx = recreate,
                Smoke = smoke,
                Strategy = "scaffold",
                CompileGatePassed = true
            });

    private static string CreateMinimalPoco()
    {
        var root = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "poco-inst-" + Guid.NewGuid().ToString("n"))).FullName;
        Directory.CreateDirectory(Path.Combine(root, "common"));
        Directory.CreateDirectory(Path.Combine(root, "poco"));
        File.WriteAllText(Path.Combine(root, "common", "processing.hpp"), "#pragma once\n");
        File.WriteAllText(Path.Combine(root, "poco", "main.cpp"), "int main(){return 0;}\n");
        File.WriteAllText(Path.Combine(root, "Dockerfile.custom"), "FROM scratch\n");
        return root;
    }

    private sealed class TrackingManagedFileSet : IManagedFileSet
    {
        private readonly IManagedFileSet _inner;
        public int RollbackCalls { get; private set; }

        public TrackingManagedFileSet(IManagedFileSet inner) => _inner = inner;

        public async Task<ManagedFileApplyResult> ApplyAsync(
            string destRoot,
            IReadOnlyDictionary<string, string> files,
            CancellationToken cancellationToken = default)
        {
            var result = await _inner.ApplyAsync(destRoot, files, cancellationToken);
            return new ManagedFileApplyResult
            {
                Succeeded = result.Succeeded,
                Rollback = async ct =>
                {
                    RollbackCalls++;
                    await result.Rollback(ct);
                }
            };
        }
    }

    private sealed class FakeOps : IPocoDeploymentOps
    {
        public int EnsureCalls { get; private set; }
        public int BuildCalls { get; private set; }
        public int RecreateCalls { get; private set; }
        public List<(string Key, string Value)> Upserts { get; } = new();
        public (bool Ok, string Log) BuildResult { get; set; } = (true, "ok");
        public (bool Ok, string Log) RecreateResult { get; set; } = (true, "healthy");
        public Func<Task>? OnBuild { get; set; }

        public void EnsureStackIdentity() => EnsureCalls++;

        public Task RetagImageAsync(string fromTag, string toTag, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public async Task<(bool Ok, string Log)> BuildCustomImageAsync(
            string pocoContext,
            string imageTag,
            CancellationToken cancellationToken)
        {
            BuildCalls++;
            if (OnBuild is not null)
                await OnBuild().ConfigureAwait(false);
            return BuildResult;
        }

        public Task<(bool Ok, string Log)> RecreateServiceAsync(
            string service,
            bool waitHealthy,
            CancellationToken cancellationToken)
        {
            RecreateCalls++;
            return Task.FromResult(RecreateResult);
        }

        public Task<(bool Ok, string Detail)> SmokeAsync(
            string? extractFile,
            CancellationToken cancellationToken) =>
            Task.FromResult((true, "health ok"));

        public void UpsertEnvVar(string key, string value) => Upserts.Add((key, value));
    }
}
