using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Transport;

namespace Nexo.Transport.A2A;

/// <summary>
/// DI composition for the outbound A2A transport. Host-composed before <c>AddNexo()</c>
/// (never kernel-registered) so the preview A2A dependency stays out of the AddNexo package
/// graph and the pack-graph alignment gate.
/// </summary>
public static class NexoA2ATransportServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="A2AAgentTransport"/> and contributes the <c>a2a+</c> endpoint scheme
    /// to the runtime's remote-transport dispatch. No-ops (registers nothing) unless
    /// <c>Nexo:A2A:Transport:Enabled</c> is true in <paramref name="configuration"/> — the
    /// enablement decision is read at composition time because the transport either
    /// participates in the kernel's transport graph or it does not.
    /// </summary>
    public static IServiceCollection AddNexoA2ATransport(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = A2ATransportOptions.SectionPath)
    {
        var section = configuration.GetSection(sectionPath);
        if (!bool.TryParse(section["Enabled"], out var enabled) || !enabled)
        {
            return services;
        }

        services.AddOptions<A2ATransportOptions>()
            .Bind(section)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<A2ATransportOptions>, ValidateA2ATransportOptions>());

        services.TryAddSingleton<A2AAgentTransport>();
        services.AddSingleton(new AgentTransportSchemeRegistration(
            A2AEndpointScheme.Prefix,
            sp => sp.GetRequiredService<A2AAgentTransport>()));

        return services;
    }
}
