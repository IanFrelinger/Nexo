using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Services;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;
using Nexo.Infrastructure.Services.AI;
using Nexo.Shared.Models;
using Nexo.CLI.Tests.Mocks;
using System;
using System.Net.Http;

namespace Nexo.CLI.Tests
{
    /// <summary>
    /// Test-specific dependency injection configuration that uses mock providers
    /// </summary>
    public static class TestDependencyInjection
    {
        /// <summary>
        /// Configures services for testing with mock AI providers
        /// </summary>
        public static IServiceCollection AddTestServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add configuration
            services.AddSingleton<IConfiguration>(configuration);
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add HTTP client
            services.AddHttpClient();

            // Add cache settings with configuration priority
            services.Configure<CacheSettings>(configuration.GetSection("Cache"));
            services.AddSingleton<CacheSettings>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var cacheSection = config.GetSection("Cache");
                
                return new CacheSettings
                {
                    Backend = cacheSection["Backend"] ?? "inmemory",
                    RedisConnectionString = cacheSection["RedisConnectionString"] ?? "localhost:6379",
                    RedisKeyPrefix = cacheSection["RedisKeyPrefix"] ?? "nexo:cache:",
                    DefaultTtlSeconds = int.TryParse(cacheSection["DefaultTtlSeconds"], out var configTtl) ? configTtl : 60
                };
            });

            // Add AI settings
            services.AddSingleton<AiSettings>(provider =>
            {
                return new AiSettings
                {
                    PreferredProvider = "mock",
                    PreferredModel = "mock-model"
                };
            });

            // Add mock AI providers instead of real ones
            services.AddSingleton<IModelProvider, MockAIProvider>();
            services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();

            // Add caching processor with mock provider
            services.AddTransient<IAsyncProcessor<ModelRequest, ModelResponse>>(provider =>
            {
                var mockProvider = provider.GetRequiredService<IModelProvider>();
                
                return new AsyncProcessor<ModelRequest, ModelResponse>(
                    async (request, cancellationToken) =>
                    {
                        return await mockProvider.ExecuteAsync(request, cancellationToken);
                    });
            });

            // Add cache strategy
            services.AddSingleton<Nexo.Core.Application.Interfaces.ICacheStrategy<string, ModelResponse>>(provider =>
            {
                var cacheSettings = provider.GetRequiredService<CacheSettings>();
                return new Nexo.Core.Application.Services.CacheStrategy<string, ModelResponse>(TimeSpan.FromSeconds(cacheSettings.DefaultTtlSeconds));
            });

            // Add caching async processor
            services.AddTransient<CachingAsyncProcessor<ModelRequest, string, ModelResponse>>(provider =>
            {
                var processor = provider.GetRequiredService<IAsyncProcessor<ModelRequest, ModelResponse>>();
                var cache = provider.GetRequiredService<Nexo.Core.Application.Interfaces.ICacheStrategy<string, ModelResponse>>();
                var logger = provider.GetRequiredService<ILogger<CachingAsyncProcessor<ModelRequest, string, ModelResponse>>>();
                
                return new CachingAsyncProcessor<ModelRequest, string, ModelResponse>(
                    processor,
                    cache,
                    request => request.Input ?? "default",
                    logger);
            });

            // Add pipeline services
            services.AddTransient<Nexo.Feature.Pipeline.Interfaces.IPipelineConfiguration, Nexo.Feature.Pipeline.Models.PipelineConfiguration>();
            services.AddTransient<Nexo.Feature.Pipeline.Interfaces.IPipelineConfigurationService, Nexo.Feature.Pipeline.Services.PipelineConfigurationService>();
            services.AddTransient<Nexo.Feature.Pipeline.Interfaces.IPipelineExecutionEngine, Nexo.Feature.Pipeline.Services.PipelineExecutionEngine>();
            services.AddTransient<Nexo.Feature.Pipeline.Interfaces.IPipelineContext>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<Nexo.Feature.Pipeline.Models.PipelineContext>>();
                var configuration = provider.GetRequiredService<Nexo.Feature.Pipeline.Interfaces.IPipelineConfiguration>();
                return new Nexo.Feature.Pipeline.Models.PipelineContext(logger, configuration, System.Threading.CancellationToken.None);
            });

            // Add resource management services for pipeline
            services.AddSingleton<Nexo.Shared.Interfaces.Resource.IResourceMonitor, Nexo.Infrastructure.Services.Resource.SystemResourceMonitor>();
            services.AddSingleton<Nexo.Shared.Interfaces.Resource.IResourceOptimizer, Nexo.Infrastructure.Services.Resource.ResourceOptimizer>();
            services.AddSingleton<Nexo.Shared.Interfaces.Resource.IResourceManager, Nexo.Infrastructure.Services.Resource.BasicResourceManager>();

            return services;
        }
    }
} 