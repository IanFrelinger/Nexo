using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions;
using Nexo.Core.Application.Sdk.Ports;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Hosting.Sdk;

/// <summary>
/// Implementation of INexoSdkBuilder. Configures NexoSdkOptions for runtime registration.
/// </summary>
public sealed class NexoSdkBuilder : INexoSdkBuilder
{
    private readonly NexoSdkOptions _options;

    /// <summary>
    /// Creates a new SDK builder with the given options.
    /// </summary>
    /// <param name="options">Options to populate.</param>
    public NexoSdkBuilder(NexoSdkOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public INexoSdkBuilder RegisterBrick<T>() where T : Brick
    {
        _options.BrickTypes.Add(typeof(T));
        return this;
    }

    /// <inheritdoc />
    public INexoSdkBuilder RegisterAgent<T>() where T : class
    {
        if (!typeof(IAgent).IsAssignableFrom(typeof(T)))
            throw new ArgumentException($"Type {typeof(T).Name} must implement {nameof(IAgent)}", nameof(T));
        _options.AgentTypes.Add(typeof(T));
        return this;
    }

    /// <inheritdoc />
    public INexoSdkBuilder RegisterAgentCard(AgentCard card)
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));
        _options.AgentCards.Add(card);
        return this;
    }
}
