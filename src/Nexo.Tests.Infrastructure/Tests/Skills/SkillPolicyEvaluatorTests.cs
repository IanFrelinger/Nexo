using FluentAssertions;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Trust.Models;
using Nexo.Infrastructure.Skills;
using Nexo.Infrastructure.Trust;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Skills;

public sealed class SkillPolicyEvaluatorTests
{
    [Fact]
    public async Task IsSkillVisible_respects_tier_allow_and_deny_lists()
    {
        using var temp = new TempTrustPackRoot();
        WritePack(temp, CreateInternalOnlyPack());
        var registry = new TrustPolicyPackRegistry(temp.Path);
        await registry.ActivateAsync("internal-only");
        var evaluator = new SkillPolicyEvaluator(registry);

        var trustedContext = CreateContext(PeerTrustTier.Trusted, "internal-only");
        var untrustedContext = CreateContext(PeerTrustTier.Untrusted, "internal-only");

        evaluator.IsSkillVisible(new NexoSkillDescriptor("nexo-trust-inspector", "x"), trustedContext)
            .Should().BeTrue();
        evaluator.IsSkillVisible(new NexoSkillDescriptor("nexo-active-policy-pack", "x"), trustedContext)
            .Should().BeTrue();
        evaluator.IsSkillVisible(new NexoSkillDescriptor("nexo-trust-inspector", "x"), untrustedContext)
            .Should().BeTrue();
        evaluator.IsSkillVisible(new NexoSkillDescriptor("nexo-active-policy-pack", "x"), untrustedContext)
            .Should().BeFalse();
    }

    [Fact]
    public async Task IsScriptAutoApproved_matches_policy_tuple()
    {
        using var temp = new TempTrustPackRoot();
        WritePack(temp, CreateInternalOnlyPack());
        var registry = new TrustPolicyPackRegistry(temp.Path);
        await registry.ActivateAsync("internal-only");
        var evaluator = new SkillPolicyEvaluator(registry);

        var trusted = CreateContext(PeerTrustTier.Trusted, "internal-only");
        var untrusted = CreateContext(PeerTrustTier.Untrusted, "internal-only");

        evaluator.IsScriptAutoApproved("nexo-trust-inspector", "scripts/list-packs.sh", trusted)
            .Should().BeTrue();
        evaluator.IsScriptAutoApproved("nexo-trust-inspector", "scripts/list-packs.sh", untrusted)
            .Should().BeFalse();
    }

    private static NexoSkillExecutionContext CreateContext(PeerTrustTier tier, string packId)
        => new("tester", "internal", tier, packId, "corr-1");

    private static TrustPolicyPack CreateInternalOnlyPack()
        => new(
            "internal-only",
            "1.0.0",
            "Internal only",
            "test",
            false,
            new Dictionary<string, bool> { ["file-contents"] = true },
            new Dictionary<string, bool> { ["shell"] = true },
            [],
            SkillRules: new SkillPolicyRules(
                new Dictionary<string, SkillTierVisibilityRules>
                {
                    ["Trusted"] = new(["nexo-trust-inspector", "nexo-active-policy-pack"], []),
                    ["Untrusted"] = new(["nexo-trust-inspector"], ["nexo-active-policy-pack"]),
                },
                new Dictionary<string, IReadOnlyList<string>>
                {
                    ["nexo-trust-inspector"] = ["scripts/list-packs.sh"],
                },
                [new SkillScriptAutoApprovalRule("nexo-trust-inspector", "scripts/list-packs.sh", ["Trusted"])],
                new SkillScriptLimits(30, 65536)));

    private static void WritePack(TempTrustPackRoot temp, TrustPolicyPack pack)
    {
        var path = Path.Combine(temp.Path, $"{pack.Id}.json");
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(pack, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        }));
    }

    private sealed class TempTrustPackRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nexo-skill-policy-" + Guid.NewGuid().ToString("N"));

        public TempTrustPackRoot() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
