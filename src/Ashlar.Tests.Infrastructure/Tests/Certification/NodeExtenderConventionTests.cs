using System.Text.Json;
using Ashlar.Core.Application.Paths;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// A3 (unattended self-extension): the shipped node must be ARMABLE into a real, autonomous
/// extender — a live model when the operator turns it on, and an arm that survives a redeploy.
/// These pin that story so it cannot silently regress to the pre-A3 no-op (an observer, or the
/// deterministic-echo provider that never calls a model).
///
/// <para>Deliberately NOT asserted: that the node ships ACTIVE. The locked owner decision is
/// Passive-by-default plus one arm command; the extender is inert until <c>background-agent mode
/// set --value active</c>. This test guards the extender's readiness, not its activation.</para>
///
/// <para>In <c>...Tests.Certification</c> so it rides cert-gate. Hermetic: pure file reads.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class NodeExtenderConventionTests
{
    private static readonly string[] MockProviders =
        { "deterministic", "mock", "mock-json", "echo", "offline" };

    private static string Root => RepoPathResolver.FindRepoRoot();

    [Fact]
    public void ShippedAgentSet_HasARealExtender_NotADeterministicNoOp()
    {
        var path = Path.Combine(Root, ".docker", "node-agents.json");
        File.Exists(path).Should().BeTrue(".docker/node-agents.json is the node's baked agent set");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var agents = doc.RootElement.GetProperty("BackgroundAgents").GetProperty("Agents");

        JsonElement? extender = null;
        foreach (var a in agents.EnumerateArray())
        {
            if (a.TryGetProperty("Role", out var role)
                && string.Equals(role.GetString(), "extender", StringComparison.OrdinalIgnoreCase))
            {
                extender = a;
                break;
            }
        }

        extender.Should().NotBeNull("the node must ship an extender agent so it can be armed to self-extend");
        var ext = extender!.Value;

        ext.GetProperty("Enabled").GetBoolean().Should().BeTrue("the extender must be enabled (it stays inert via Passive mode, not by being disabled)");

        var provider = ext.GetProperty("ModelProvider").GetString();
        provider.Should().NotBeNullOrWhiteSpace();
        MockProviders.Should().NotContain(
            p => string.Equals(p, provider, StringComparison.OrdinalIgnoreCase),
            "the shipped extender must name a REAL model provider — a deterministic/mock provider never calls a model, so an armed node would self-extend nothing (A3). Provider was '{0}'", provider!);

        ext.GetProperty("Parameters").TryGetProperty("RepoRoot", out var repoRoot).Should().BeTrue(
            "the extender needs a RepoRoot, or the runner claims nothing (BackgroundAgentRegistry only takes the extender branch when RepoRoot/Path is present)");
        repoRoot.GetString().Should().StartWith("/data/state", "the extender's project must live on the state volume so its work and gate history persist");
    }

    [Fact]
    public void Node_ArmIsDurable_ModePathOnTheStateVolume()
    {
        // The aggressiveness mode file must live on the state volume, or `background-agent mode set
        // --value active` writes to the container layer and a docker rm / redeploy silently reverts
        // the node to Passive — the arm would not survive, and the operator would not know.
        var dockerfile = Path.Combine(Root, ".docker", "Dockerfile.cli");
        var text = File.ReadAllText(dockerfile);

        text.Should().MatchRegex(
            @"ASHLAR_AGENT_MODE_PATH\s*=\s*/data/state/\S+",
            "Dockerfile.cli must set ASHLAR_AGENT_MODE_PATH under /data/state so the arm survives a redeploy");
    }
}
