using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.ModelArtifacts.Ports;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;

namespace Nexo.Infrastructure.ModelArtifacts;

public static class ModelArtifactCatalogServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IModelArtifactCatalogService"/> and the default Ollama <c>/api/tags</c> source.
    /// Call <see cref="AddDockerOllamaModelArtifactCatalogSource"/> from desktop host registrations when Docker discovery is desired.
    /// </summary>
    public static IServiceCollection AddModelArtifactCatalog(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<OllamaBackendOptions>()
            .Bind(configuration.GetSection(OllamaBackendOptions.SectionName));
        services.AddOptions<DockerOllamaModelArtifactCatalogOptions>()
            .Bind(configuration.GetSection(DockerOllamaModelArtifactCatalogOptions.SectionName));

        services.AddHttpClient(OllamaTagsModelArtifactCatalogSource.HttpClientName, (sp, client) =>
        {
            var ollama = sp.GetRequiredService<IOptions<OllamaBackendOptions>>().Value;
            client.BaseAddress = new Uri(ollama.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient(DockerOllamaModelArtifactCatalogSource.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
        });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IModelArtifactCatalogSource, OllamaTagsModelArtifactCatalogSource>());
        services.TryAddSingleton<IModelArtifactCatalogService, ModelArtifactCatalogService>();
        return services;
    }

    /// <summary>
    /// Adds Docker engine probing for Ollama containers (same HTTP <c>/api/tags</c> per published port).
    /// </summary>
    public static IServiceCollection AddDockerOllamaModelArtifactCatalogSource(this IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IModelArtifactCatalogSource, DockerOllamaModelArtifactCatalogSource>());
        return services;
    }
}
