using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Sdk.Client;

/// <summary>
/// Fluent builder for HTTP client–oriented Nexo integration (Unity, Unreal, embedded hosts talking to a Nexo API).
/// For registering bricks/agents inside the Nexo kernel process, use <c>Nexo.Hosting.Sdk.AddNexoSdk</c> before <c>AddNexo</c>.
/// </summary>
public class NexoClientSdkBuilder
{
    /// <summary>Creates a builder bound to the caller's <see cref="IServiceCollection"/>.</summary>
    protected internal NexoClientSdkBuilder(IServiceCollection services) => Services = services;

    /// <summary>Services being configured.</summary>
    protected internal IServiceCollection Services { get; }
}
