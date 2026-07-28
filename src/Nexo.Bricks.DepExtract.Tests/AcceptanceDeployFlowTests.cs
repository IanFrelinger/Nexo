using FluentAssertions;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Infrastructure.Execution.Scratch;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Part (a): the acceptance evaluator owns the post-smoke ship/rollback verdict.
/// A Rollback decision must drive the SAME rollback path a failed build drives —
/// IManagedFileSet.Rollback plus prior-image restore.
/// </summary>
public sealed class AcceptanceDeployFlowTests
{
    [Fact]
    public async Task RollbackDecision_RollsBackManagedFileSet_AndRestoresPriorImage()
    {
        var poco = CreateMinimalPoco();
        var prior = Path.Combine(poco, "common", "adapted_reader.hpp");
        await File.WriteAllTextAsync(prior, "PRIOR_ADAPTER");

        var ops = new RecordingOps { SmokeResult = (true, "health+extract ok rows=1 job=j1") };
        var files = new TrackingManagedFileSet(new SnapshotManagedFileSet());
        var evaluator = new ScriptedAcceptanceEvaluator(
            AcceptanceResult.Rollback("row count below acceptance floor", new[] { "rows=1" }));

        var target = CreateTarget(poco, ops, files, smoke: true);

        var result = await target.ApplyAsync(
            new GeneratedArtifact { Content = "#pragma once\nclass AdaptedReader {};\n" },
            evaluator);

        // The install itself is reported as failed and rolled back.
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("acceptance");
        result.Message.Should().Contain("row count below acceptance floor");

        // File set rolled back to the pre-apply snapshot.
        files.RollbackCalls.Should().Be(1);
        (await File.ReadAllTextAsync(prior)).Should().Be("PRIOR_ADAPTER");

        // Prior image tag restored (evtx:custom-prev -> evtx:custom).
        ops.Retags.Should().Contain(("evtx:custom-prev", "evtx:custom"));

        // Evaluator saw the captured smoke output — it never ran the smoke itself.
        evaluator.Calls.Should().Be(1);
        evaluator.LastContext!.Smoke.Ran.Should().BeTrue();
        evaluator.LastContext.Smoke.Succeeded.Should().BeTrue();
        evaluator.LastContext.Smoke.Detail.Should().Contain("rows=1");
    }

    [Fact]
    public async Task ShipDecision_Commits_WithoutRollback()
    {
        var poco = CreateMinimalPoco();
        var ops = new RecordingOps { SmokeResult = (true, "health+extract ok rows=42 job=j1") };
        var files = new TrackingManagedFileSet(new SnapshotManagedFileSet());
        var evaluator = new ScriptedAcceptanceEvaluator(
            AcceptanceResult.Ship("rows=42 within band", new[] { "rows=42" }));

        var target = CreateTarget(poco, ops, files, smoke: true);

        var result = await target.ApplyAsync(
            new GeneratedArtifact { Content = "#pragma once\nclass AdaptedReader {};\n" },
            evaluator);

        result.Succeeded.Should().BeTrue();
        files.RollbackCalls.Should().Be(0);
        ops.Retags.Should().NotContain(("evtx:custom-prev", "evtx:custom"));
        File.Exists(Path.Combine(poco, "common", "adapted_reader.hpp")).Should().BeTrue();
        evaluator.Calls.Should().Be(1);
    }

    [Fact]
    public async Task NoEvaluatorSupplied_DefaultRollsBack_WhenSmokeFails()
    {
        var poco = CreateMinimalPoco();
        var prior = Path.Combine(poco, "common", "adapted_reader.hpp");
        await File.WriteAllTextAsync(prior, "PRIOR_ADAPTER");

        var ops = new RecordingOps { SmokeResult = (false, "extract produced no data rows") };
        var files = new TrackingManagedFileSet(new SnapshotManagedFileSet());
        var target = CreateTarget(poco, ops, files, smoke: true);

        // Plain IDeploymentTarget entry point: no evaluator supplied at all.
        var result = await target.ApplyAsync(
            new GeneratedArtifact { Content = "#pragma once\nclass AdaptedReader {};\n" });

        result.Succeeded.Should().BeFalse();
        files.RollbackCalls.Should().Be(1);
        (await File.ReadAllTextAsync(prior)).Should().Be("PRIOR_ADAPTER");
        ops.Retags.Should().Contain(("evtx:custom-prev", "evtx:custom"));
    }

    [Fact]
    public async Task SmokeNotRun_DefaultShips()
    {
        var poco = CreateMinimalPoco();
        var ops = new RecordingOps();
        var files = new TrackingManagedFileSet(new SnapshotManagedFileSet());
        var target = CreateTarget(poco, ops, files, smoke: false);

        var result = await target.ApplyAsync(
            new GeneratedArtifact { Content = "#pragma once\nclass AdaptedReader {};\n" });

        result.Succeeded.Should().BeTrue();
        files.RollbackCalls.Should().Be(0);
    }

    private static PocoInstallTarget CreateTarget(
        string poco,
        IPocoDeploymentOps ops,
        IManagedFileSet files,
        bool smoke) =>
        new(
            files,
            new SingleFlightGuard(),
            ops,
            new PocoInstallTargetOptions
            {
                PocoContext = poco,
                RebuildImage = true,
                RecreateEvtx = true,
                Smoke = smoke,
                Strategy = "scaffold",
                CompileGatePassed = true
            });

    private static string CreateMinimalPoco()
    {
        var root = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "accept-" + Guid.NewGuid().ToString("n"))).FullName;
        Directory.CreateDirectory(Path.Combine(root, "common"));
        Directory.CreateDirectory(Path.Combine(root, "poco"));
        File.WriteAllText(Path.Combine(root, "common", "processing.hpp"), "#pragma once\n");
        File.WriteAllText(Path.Combine(root, "poco", "main.cpp"), "int main(){return 0;}\n");
        File.WriteAllText(Path.Combine(root, "Dockerfile.custom"), "FROM scratch\n");
        return root;
    }

    private sealed class ScriptedAcceptanceEvaluator : IAcceptanceEvaluator
    {
        private readonly AcceptanceResult _result;
        public int Calls { get; private set; }
        public AcceptanceContext? LastContext { get; private set; }

        public ScriptedAcceptanceEvaluator(AcceptanceResult result) => _result = result;

        public AcceptanceResult Evaluate(AcceptanceContext context)
        {
            Calls++;
            LastContext = context;
            return _result;
        }
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

    private sealed class RecordingOps : IPocoDeploymentOps
    {
        public List<(string From, string To)> Retags { get; } = new();
        public (bool Ok, string Log) BuildResult { get; set; } = (true, "ok");
        public (bool Ok, string Log) RecreateResult { get; set; } = (true, "healthy");
        public (bool Ok, string Detail) SmokeResult { get; set; } = (true, "health ok");

        public void EnsureStackIdentity() { }

        public Task RetagImageAsync(string fromTag, string toTag, CancellationToken cancellationToken)
        {
            Retags.Add((fromTag, toTag));
            return Task.CompletedTask;
        }

        public Task<(bool Ok, string Log)> BuildCustomImageAsync(
            string pocoContext, string imageTag, CancellationToken cancellationToken) =>
            Task.FromResult(BuildResult);

        public Task<(bool Ok, string Log)> RecreateServiceAsync(
            string service, bool waitHealthy, CancellationToken cancellationToken) =>
            Task.FromResult(RecreateResult);

        public Task<(bool Ok, string Detail)> SmokeAsync(
            string? extractFile, CancellationToken cancellationToken) =>
            Task.FromResult(SmokeResult);

        public void UpsertEnvVar(string key, string value) { }
    }
}
