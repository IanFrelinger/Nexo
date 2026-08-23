using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.Registry;

namespace Ashlar.Tests.BackgroundAgents.Registry;

/// <summary>
/// Test helpers for boot-simulation registrations that mirror human-authored paths.
/// </summary>
internal static class BackgroundAgentRegistryTestExtensions
{
    internal static Task RegisterAuthoredAsync(
        this IBackgroundAgentRegistry registry,
        IAgent agent,
        BackgroundAgentConfig config,
        CancellationToken cancellationToken = default) =>
        registry.RegisterAsync(agent, config, AgentRegistrationOrigin.Authored, cancellationToken);
}
