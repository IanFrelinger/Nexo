using Microsoft.Extensions.DependencyInjection;

namespace Ashlar.Sdk.Client;

/// <summary>
/// Fluent builder for HTTP client–oriented Ashlar integration (Unity, Unreal, embedded hosts talking to a Ashlar API).
/// For registering bricks/agents inside the Ashlar kernel process, use <c>Ashlar.Hosting.Sdk.AddAshlarSdk</c> before <c>AddAshlar</c>.
/// </summary>
public class AshlarClientSdkBuilder
{
    /// <summary>Creates a builder bound to the caller's <see cref="IServiceCollection"/>.</summary>
    protected internal AshlarClientSdkBuilder(IServiceCollection services) => Services = services;

    /// <summary>Services being configured.</summary>
    protected internal IServiceCollection Services { get; }
}
