using A2ACard = A2A.AgentCard;
using A2ACapabilities = A2A.AgentCapabilities;
using A2AInterface = A2A.AgentInterface;
using A2ASkill = A2A.AgentSkill;

namespace Nexo.Transport.A2A.Server;

/// <summary>
/// Projects Nexo agent descriptors onto A2A-spec agent cards.
///
/// Naming note: <c>A2A.AgentCard</c> (the spec discovery document) collides with Nexo's own
/// <c>Nexo.Core.Domain.Agents.AgentCard</c> persona model. The spec type is aliased and never
/// leaves the A2A adapter projects; the domain type is untouched.
/// </summary>
public interface INexoA2ACardProjector
{
    /// <summary>Builds the spec card for one exposed agent.</summary>
    A2ACard Project(NexoA2AAgentDescriptor descriptor, NexoA2AServerOptions options, string basePattern);
}

/// <summary>Default projector.</summary>
public sealed class NexoA2ACardProjector : INexoA2ACardProjector
{
    private static readonly List<string> DefaultModes = ["text/plain", "application/json"];

    /// <inheritdoc />
    public A2ACard Project(NexoA2AAgentDescriptor descriptor, NexoA2AServerOptions options, string basePattern)
    {
        var baseUrl = options.PublicBaseUrl?.TrimEnd('/') ?? string.Empty;
        var endpointUrl = $"{baseUrl}{basePattern}/{Uri.EscapeDataString(descriptor.Id)}";

        var skills = new List<A2ASkill>();
        foreach (var behavior in descriptor.Behaviors)
        {
            skills.Add(new A2ASkill
            {
                Id = behavior,
                Name = behavior,
                Description = $"Behavior '{behavior}' of agent '{descriptor.Name}'.",
                Tags = string.IsNullOrWhiteSpace(descriptor.Domain) ? [] : [descriptor.Domain!],
            });
        }

        return new A2ACard
        {
            Name = descriptor.Name,
            Description = descriptor.Description,
            Version = descriptor.Version,
            SupportedInterfaces =
            [
                new A2AInterface
                {
                    Url = endpointUrl,
                    ProtocolBinding = global::A2A.ProtocolBindingNames.JsonRpc,
                },
            ],
            // v1 is synchronous by design: no streaming, no push notifications — see
            // NexoA2AServerOptions class docs.
            Capabilities = new A2ACapabilities
            {
                Streaming = false,
                PushNotifications = false,
            },
            Skills = skills,
            DefaultInputModes = DefaultModes,
            DefaultOutputModes = DefaultModes,
        };
    }
}
