using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Skills;
using Nexo.Infrastructure.Skills.Sdk.Extensions;
using Nexo.Infrastructure.Trust;
using Nexo.Skills.AgentFramework;
using Nexo.Skills.AgentFramework.ClassSkills;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Skills;

public sealed class SkillAgentBridgeTests
{
    [Fact]
    public async Task BuildSkillInstructionsAsync_includes_visible_skills()
    {
        var repoRoot = Nexo.Core.Application.Paths.RepoPathResolver.FindRepoRoot();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITrustPolicyPackRegistry>(_ =>
            new TrustPolicyPackRegistry(Path.Combine(repoRoot, "config", "trust-packs")));
        services.AddSingleton<Nexo.Abstractions.Barriers.IBarrierContextAccessor, StubBarrier>();
        services.AddNexoSkills();
        services.AddNexoAgentFrameworkSkills(options =>
            options.SkillRootPaths.Add(Path.Combine(repoRoot, "skills")));

        using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<ITrustPolicyPackRegistry>().ActivateAsync("internal-only");
        var bridge = provider.GetRequiredService<INexoSkillAgentBridge>();
        var ctxFactory = provider.GetRequiredService<INexoSkillExecutionContextFactory>();

        var text = await bridge.BuildSkillInstructionsAsync(
            ctxFactory.Create(PeerTrustTier.Trusted, "tester"));

        text.Should().Contain("nexo-trust-inspector");
        text.Should().Contain("Available Nexo skills");
        provider.GetRequiredService<Microsoft.Agents.AI.AgentSkillsProvider>().Should().NotBeNull();
    }

    private sealed class StubBarrier : Nexo.Abstractions.Barriers.IBarrierContextAccessor
    {
        public Nexo.Abstractions.Barriers.BarrierContext? Current { get; private set; }
        public void Initialize(Nexo.Abstractions.Barriers.BarrierContext context) => Current = context;
    }
}
