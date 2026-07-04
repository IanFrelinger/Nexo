#pragma warning disable CS0618 // intentional obsolete forwarding surface for NuGet compatibility

using Microsoft.Extensions.DependencyInjection;
using Nexo.Sdk.Client;

namespace Nexo.Sdk.Legacy;

/// <summary>Obsolete entry points preserved for binary compatibility.</summary>
public static class NexoSdkLegacyExtensions
{
    /// <inheritdoc cref="NexoClientSdkServiceCollectionExtensions.AddNexoClientSdk"/>
    [Obsolete("Use Nexo.Sdk.Client.NexoClientSdkServiceCollectionExtensions.AddNexoClientSdk.", error: false)]
    public static IServiceCollection AddNexoSdk(
        this IServiceCollection services,
        string baseUrl,
        Action<NexoSdkBuilder>? configure = null)
    {
        return NexoClientSdkServiceCollectionExtensions.AddNexoClientSdk(
            services,
            baseUrl,
            configure == null ? null : b => configure(new NexoSdkBuilder(b.Services)));
    }
}
