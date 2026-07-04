using Nexo.Abstractions;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure.Sdk.Ports;

namespace Nexo.Hosting.Sdk.Builders;
#pragma warning disable CS0618 // NexoSdkBuilder is an obsolete type forwarder in this file

/// <summary>
/// Default implementation of <see cref="INexoSdkBuilder"/>. Configures <see cref="NexoSdkOptions"/> for kernel registration.
/// </summary>
public class HostNexoSdkBuilder : INexoSdkBuilder
{
    private readonly NexoSdkOptions _options;

    /// <summary>
    /// Creates a new SDK builder with the given options.
    /// </summary>
    /// <param name="options">Options to populate.</param>
    public HostNexoSdkBuilder(NexoSdkOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public INexoSdkBuilder RegisterBrick<T>() where T : DomainBrick
    {
        _options.BrickTypes.Add(typeof(T));
        return this;
    }

    /// <inheritdoc />
    public INexoSdkBuilder RegisterAgent<T>() where T : class
    {
        if (!typeof(IAgent).IsAssignableFrom(typeof(T)))
        {
            throw new ArgumentException($"Type {typeof(T).Name} must implement {nameof(IAgent)}", nameof(T));
        }

        _options.AgentTypes.Add(typeof(T));
        return this;
    }

    /// <inheritdoc />
    public INexoSdkBuilder RegisterAgentCard(AgentCard card)
    {
        if (card == null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        _options.AgentCards.Add(card);
        return this;
    }
}
