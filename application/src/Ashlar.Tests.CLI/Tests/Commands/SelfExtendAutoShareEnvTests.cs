using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.Manifest.Signing;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// Pins the ENV half of auto-share — the production activation switch
/// (<c>ASHLAR_MESH_AUTOSHARE=1</c> with the store resolved from <c>ASHLAR_MESH_DIR</c>) and the
/// unsigned-admission skip. These live in THIS assembly because it disables xunit parallelism
/// (AssemblyParallelism.cs), the one place env mutation with save/restore is safe; the
/// kernel and bridge test projects run parallel and inject parameters instead.
/// </summary>
[Trait("Category", "CLI")]
public sealed class SelfExtendAutoShareEnvTests : IDisposable
{
    private readonly string _repo;

    public SelfExtendAutoShareEnvTests()
    {
        _repo = Path.Combine(Path.GetTempPath(), "autoshare-env-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_repo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repo))
        {
            Directory.Delete(_repo, recursive: true);
        }
    }

    private void WriteSelfExtendingPolicy() =>
        File.WriteAllText(Path.Combine(_repo, "ashlar.policy.yaml"), """
            apiVersion: ashlar/v1
            kind: Policy
            sandbox:
              root: .
              writable: []
            selfExtend:
              mode: self-extending
              budget:
                extensions: 3
                window: 24h
              mayAdd: [brick]
              gatesRequired: [sandbox]
            never:
              - modify_gate
              - widen_sandbox
              - access_signing_keys
              - truncate_ledger
              - grant_capability
            """);

    private string ParkForgeWrite()
    {
        var forge = AshlarProjectMediation.ProjectStore(_repo);
        return forge.Add(new ChangeProposal
        {
            Id = "forge-" + Guid.NewGuid().ToString("N")[..8],
            TargetPath = "src/Coprod.cs",
            NewContent = "// coprod via env",
            Summary = "parked by the cycle",
            CreatedAt = DateTimeOffset.UtcNow,
        }).Id;
    }

    [Fact]
    public async Task The_env_switch_activates_auto_share_with_the_store_from_ASHLAR_MESH_DIR()
    {
        WriteSelfExtendingPolicy();
        var signer = OperatorKey.Generate(Path.Combine(_repo, "keys"));
        var forgeId = ParkForgeWrite();
        var meshRoot = Path.Combine(_repo, "mesh-root");

        var prevShare = Environment.GetEnvironmentVariable("ASHLAR_MESH_AUTOSHARE");
        var prevDir = Environment.GetEnvironmentVariable("ASHLAR_MESH_DIR");
        try
        {
            Environment.SetEnvironmentVariable("ASHLAR_MESH_AUTOSHARE", "1");
            Environment.SetEnvironmentVariable("ASHLAR_MESH_DIR", meshRoot);

            // autoShare and meshDir both null: the env alone decides — the production path.
            var outcome = await SelfExtendAdmissionBridge.TryRecordAsync(
                _repo, "night-agent", "co-produce via env", writePaths: [],
                toolCallsExecuted: 2, toolCallsDenied: 0, NullLogger.Instance,
                forgeProposalIds: [forgeId], signer: signer);

            outcome.Should().Contain("admitted").And.Contain("shared");
            Directory.EnumerateFiles(Path.Combine(meshRoot, "published"), "*.ashpkg").Should().ContainSingle(
                "the env switch must land the package in $ASHLAR_MESH_DIR/published");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASHLAR_MESH_AUTOSHARE", prevShare);
            Environment.SetEnvironmentVariable("ASHLAR_MESH_DIR", prevDir);
        }
    }

    [Fact]
    public async Task An_unsigned_admission_skips_auto_share_and_says_why()
    {
        // SPEC-006: a package's seal is a signature — an unsigned admission cannot travel. The
        // skip must be honest (annotated) and non-fatal (the admission and its write stand).
        WriteSelfExtendingPolicy();
        var forgeId = ParkForgeWrite();
        var mesh = Path.Combine(_repo, "mesh");

        var prevKeys = Environment.GetEnvironmentVariable("ASHLAR_KEY_DIR");
        try
        {
            // No key anywhere the bridge can load one from: the admission records unsigned.
            Environment.SetEnvironmentVariable("ASHLAR_KEY_DIR", Path.Combine(_repo, "no-keys"));

            var outcome = await SelfExtendAdmissionBridge.TryRecordAsync(
                _repo, "night-agent", "co-produce unsigned", writePaths: [],
                toolCallsExecuted: 2, toolCallsDenied: 0, NullLogger.Instance,
                forgeProposalIds: [forgeId], signer: null, autoShare: true, meshDir: mesh);

            outcome.Should().Contain("admitted").And.Contain("auto-share skipped: unsigned");
            Directory.Exists(mesh).Should().BeFalse("an unsigned admission must not reach the mesh");
            File.Exists(Path.Combine(_repo, "src", "Coprod.cs")).Should().BeTrue("the admission itself stands");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASHLAR_KEY_DIR", prevKeys);
        }
    }
}
