using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Maintenance.Models;
using Nexo.Core.Application.Maintenance.Ports;
using Nexo.Infrastructure.Maintenance.Adapters;
using Nexo.Infrastructure.Maintenance.Ports;
using Nexo.Infrastructure.Maintenance.Strategies;

namespace Nexo.Infrastructure.Maintenance;

/// <summary>
/// DI extensions for artifact cleanup services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers artifact cleanup services. TestArtifactCleanupStrategy is always registered.
    /// IncompleteBlobCleanupStrategy is registered when NEXO_INCOMPLETE_BLOB_PATH is set.
    /// DockerDesktopLifecycleAdapter is registered when NEXO_BLOB_LIFECYCLE=docker.
    /// </summary>
    public static IServiceCollection AddArtifactCleanup(this IServiceCollection services)
    {

        services.Configure<ArtifactCleanupOptions>(opts =>
        {
            opts.CleanBeforeTest = string.Equals(Environment.GetEnvironmentVariable("NEXO_CLEAN_BEFORE_TEST"), "1", StringComparison.OrdinalIgnoreCase);
            opts.CleanAfterTest = string.Equals(Environment.GetEnvironmentVariable("NEXO_CLEAN_AFTER_TEST"), "1", StringComparison.OrdinalIgnoreCase);
            opts.RepoRoot = Environment.GetEnvironmentVariable("NEXO_ARTIFACT_CLEANUP_REPO_ROOT");
        });

        services.Configure<IncompleteBlobCleanupOptions>(opts =>
        {
            opts.BlobStoragePath = Environment.GetEnvironmentVariable("NEXO_INCOMPLETE_BLOB_PATH");
        });

        services.AddSingleton<IArtifactCleanupStrategy, TestArtifactCleanupStrategy>();

        var blobPath = Environment.GetEnvironmentVariable("NEXO_INCOMPLETE_BLOB_PATH");
        var blobLifecycle = Environment.GetEnvironmentVariable("NEXO_BLOB_LIFECYCLE");
        if (!string.IsNullOrWhiteSpace(blobPath))
        {
            if (string.Equals(blobLifecycle, "docker", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IBlobStorageLifecycle, DockerDesktopLifecycleAdapter>();
            }
            else
            {
                services.AddSingleton<IBlobStorageLifecycle, NullBlobStorageLifecycle>();
            }
            services.AddSingleton<IArtifactCleanupStrategy, IncompleteBlobCleanupStrategy>();
        }

        services.AddSingleton<IArtifactCleanupService>(sp =>
        {
            var strategies = sp.GetServices<IArtifactCleanupStrategy>().ToList();
            var logger = sp.GetRequiredService<ILogger<ArtifactCleanupService>>();
            return new ArtifactCleanupService(strategies, logger);
        });

        return services;
    }
}
