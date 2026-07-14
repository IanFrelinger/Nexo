using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Nexo.AI.Pipeline;
using Nexo.AI.Pipeline.Clients;
using Nexo.AI.Pipeline.Governance;
using Nexo.AI.Pipeline.Routing;
using Xunit;

namespace Nexo.Tests.AI.Pipeline;

public sealed class RoutingChatClientTests
{
    private sealed class AllowListPolicy : IChatTargetAccessPolicy
    {
        private readonly HashSet<string> _allowed;

        public AllowListPolicy(params string[] allowed) =>
            _allowed = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);

        public bool IsAllowed(string? callerIdentity, string targetKey, string? modelId, out string? denyReason)
        {
            if (_allowed.Contains(targetKey))
            {
                denyReason = null;
                return true;
            }

            denyReason = $"not in allow-list: {targetKey}";
            return false;
        }
    }

    [Fact]
    public async Task Policy_forbids_cloud_never_consults_cloud_spy()
    {
        var localSpy = new SpyChatClient("local");
        var cloudSpy = new SpyChatClient("cloud");
        var availability = new InMemoryTargetAvailability()
            .Set(MeaiTargetKeys.LocalOllama, true)
            .Set(MeaiTargetKeys.LocalOnnx, false)
            .Set(DefaultRouteCandidateTable.CloudBedrockBalanced, true);

        var services = new ServiceCollection();
        services.AddSingleton<IChatTargetAccessPolicy>(new AllowListPolicy(MeaiTargetKeys.LocalOllama, MeaiTargetKeys.LocalOnnx));
        services.AddSingleton<IChatMessageSanitizer, DefaultChatMessageSanitizer>();
        services.AddSingleton<ITargetSanitizePolicy, DefaultTargetSanitizePolicy>();
        services.AddSingleton<IChatInvocationAuditor, InMemoryChatInvocationAuditor>();
        services.AddSingleton<ITargetAvailability>(availability);
        services.AddSingleton<IRouteCandidateTable, DefaultRouteCandidateTable>();
        services.AddSingleton<IChatRouter, LocalFirstChatRouter>();

        services.AddNexoGovernedChatClient(MeaiTargetKeys.LocalOllama, _ => localSpy);
        services.AddNexoGovernedChatClient(MeaiTargetKeys.LocalOnnx, _ => new SpyChatClient("onnx"));
        services.AddNexoGovernedChatClient(DefaultRouteCandidateTable.CloudBedrockBalanced, _ => cloudSpy);

        services.AddSingleton(sp => ActivatorUtilities.CreateInstance<RoutingChatClient>(sp));

        await using var provider = services.BuildServiceProvider();
        var router = provider.GetRequiredService<RoutingChatClient>();

        var options = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [RouteRequestHints.CapabilityTierKey] = "balanced",
            },
        };

        var response = await router.GetResponseAsync("hello", options);
        response.Text.Should().Be("local");
        localSpy.CallCount.Should().Be(1);
        cloudSpy.CallCount.Should().Be(0);
        router.LastDecision!.ChosenTargetKey.Should().Be(MeaiTargetKeys.LocalOllama);
        router.LastDecision.CandidatesConsidered.Should()
            .NotContain(c => c.TargetKey.StartsWith("cloud:", StringComparison.OrdinalIgnoreCase)
                             && c.Disposition.StartsWith("choose:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Local_down_cloud_allowed_falls_back_with_audited_reason()
    {
        var localSpy = new SpyChatClient("local");
        var cloudSpy = new SpyChatClient("cloud");
        var auditor = new InMemoryChatInvocationAuditor();
        var availability = new InMemoryTargetAvailability()
            .Set(MeaiTargetKeys.LocalOllama, false)
            .Set(MeaiTargetKeys.LocalOnnx, false)
            .Set(DefaultRouteCandidateTable.CloudBedrockBalanced, true);

        var services = new ServiceCollection();
        var policy = new DefaultChatTargetAccessPolicy();
        policy.AllowedCloudTargets.Add(DefaultRouteCandidateTable.CloudBedrockBalanced);
        services.AddSingleton<IChatTargetAccessPolicy>(policy);
        services.AddSingleton<IChatMessageSanitizer, DefaultChatMessageSanitizer>();
        services.AddSingleton<ITargetSanitizePolicy, DefaultTargetSanitizePolicy>();
        services.AddSingleton<IChatInvocationAuditor>(auditor);
        services.AddSingleton<ITargetAvailability>(availability);
        services.AddSingleton<IRouteCandidateTable, DefaultRouteCandidateTable>();
        services.AddSingleton<IChatRouter, LocalFirstChatRouter>();

        services.AddNexoGovernedChatClient(MeaiTargetKeys.LocalOllama, _ => localSpy);
        services.AddNexoGovernedChatClient(MeaiTargetKeys.LocalOnnx, _ => new SpyChatClient("onnx"));
        services.AddNexoGovernedChatClient(DefaultRouteCandidateTable.CloudBedrockBalanced, _ => cloudSpy);
        services.AddSingleton(sp => ActivatorUtilities.CreateInstance<RoutingChatClient>(sp));

        await using var provider = services.BuildServiceProvider();
        var router = provider.GetRequiredService<RoutingChatClient>();

        var options = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [RouteRequestHints.CapabilityTierKey] = nameof(RouteCapabilityTier.Balanced),
            },
        };

        var response = await router.GetResponseAsync("hello", options);
        response.Text.Should().Be("cloud");
        localSpy.CallCount.Should().Be(0);
        cloudSpy.CallCount.Should().Be(1);
        router.LastDecision!.ChosenTargetKey.Should().Be(DefaultRouteCandidateTable.CloudBedrockBalanced);
        router.LastDecision.ReasonCode.Should().Be(RouteReasonCodes.Fallback);
        auditor.Records.Should().Contain(r =>
            r.TargetKey == RoutingChatClient.RouterTargetKey
            && r.ReasonCode == RouteReasonCodes.Fallback);
    }

    [Fact]
    public async Task Local_down_cloud_forbidden_fails_without_silent_degradation()
    {
        var cloudSpy = new SpyChatClient("cloud");
        var availability = new InMemoryTargetAvailability()
            .Set(MeaiTargetKeys.LocalOllama, false)
            .Set(MeaiTargetKeys.LocalOnnx, false)
            .Set(DefaultRouteCandidateTable.CloudBedrockBalanced, true);

        var services = new ServiceCollection();
        services.AddSingleton<IChatTargetAccessPolicy>(new AllowListPolicy(MeaiTargetKeys.LocalOllama, MeaiTargetKeys.LocalOnnx));
        services.AddSingleton<IChatMessageSanitizer, DefaultChatMessageSanitizer>();
        services.AddSingleton<ITargetSanitizePolicy, DefaultTargetSanitizePolicy>();
        services.AddSingleton<IChatInvocationAuditor, InMemoryChatInvocationAuditor>();
        services.AddSingleton<ITargetAvailability>(availability);
        services.AddSingleton<IRouteCandidateTable, DefaultRouteCandidateTable>();
        services.AddSingleton<IChatRouter, LocalFirstChatRouter>();

        services.AddNexoGovernedChatClient(MeaiTargetKeys.LocalOllama, _ => new SpyChatClient("local"));
        services.AddNexoGovernedChatClient(MeaiTargetKeys.LocalOnnx, _ => new SpyChatClient("onnx"));
        services.AddNexoGovernedChatClient(DefaultRouteCandidateTable.CloudBedrockBalanced, _ => cloudSpy);
        services.AddSingleton(sp => ActivatorUtilities.CreateInstance<RoutingChatClient>(sp));

        await using var provider = services.BuildServiceProvider();
        var router = provider.GetRequiredService<RoutingChatClient>();

        var act = async () => await router.GetResponseAsync("hello");
        var ex = await act.Should().ThrowAsync<PolicyViolationException>();
        ex.Which.Code.Should().Be(RouteReasonCodes.NoCandidate);
        cloudSpy.CallCount.Should().Be(0);
    }

    public static IEnumerable<object[]> PolicyAvailabilityMatrix()
    {
        // cloudAllowed, ollamaUp, onnxUp, cloudUp, tier, expectedTargetOrEmpty, expectedReason
        yield return new object[] { false, true, true, true, RouteCapabilityTier.Fast, MeaiTargetKeys.LocalOllama, RouteReasonCodes.PreferredLocal };
        yield return new object[] { false, false, true, true, RouteCapabilityTier.Fast, MeaiTargetKeys.LocalOnnx, RouteReasonCodes.Fallback };
        yield return new object[] { false, false, false, true, RouteCapabilityTier.Balanced, "", RouteReasonCodes.NoCandidate };
        yield return new object[] { true, false, false, true, RouteCapabilityTier.Balanced, DefaultRouteCandidateTable.CloudBedrockBalanced, RouteReasonCodes.Fallback };
        yield return new object[] { true, false, false, true, RouteCapabilityTier.Heavy, DefaultRouteCandidateTable.CloudBedrockBalanced, RouteReasonCodes.CapabilityEscalation };
        yield return new object[] { true, false, false, false, RouteCapabilityTier.Heavy, "", RouteReasonCodes.NoCandidate };
        yield return new object[] { true, true, false, true, RouteCapabilityTier.Heavy, MeaiTargetKeys.LocalOllama, RouteReasonCodes.PreferredLocal };
    }

    [Theory]
    [MemberData(nameof(PolicyAvailabilityMatrix))]
    public void Matrix_selects_expected_target(
        bool cloudAllowed,
        bool ollamaUp,
        bool onnxUp,
        bool cloudUp,
        RouteCapabilityTier tier,
        string expectedTarget,
        string expectedReason)
    {
        var availability = new InMemoryTargetAvailability()
            .Set(MeaiTargetKeys.LocalOllama, ollamaUp)
            .Set(MeaiTargetKeys.LocalOnnx, onnxUp)
            .Set(DefaultRouteCandidateTable.CloudBedrockFast, cloudUp)
            .Set(DefaultRouteCandidateTable.CloudBedrockBalanced, cloudUp)
            .Set(DefaultRouteCandidateTable.CloudBedrockHeavy, cloudUp);

        var policy = new DefaultChatTargetAccessPolicy();
        if (cloudAllowed)
        {
            policy.AllowedCloudTargets.Add(DefaultRouteCandidateTable.CloudBedrockFast);
            policy.AllowedCloudTargets.Add(DefaultRouteCandidateTable.CloudBedrockBalanced);
            policy.AllowedCloudTargets.Add(DefaultRouteCandidateTable.CloudBedrockHeavy);
        }

        var router = new LocalFirstChatRouter(new DefaultRouteCandidateTable(), availability, policy);
        var decision = router.Select(callerIdentity: null, modelId: null, tier, preferredTarget: null);

        decision.ChosenTargetKey.Should().Be(expectedTarget);
        decision.ReasonCode.Should().Be(expectedReason);
    }
}
