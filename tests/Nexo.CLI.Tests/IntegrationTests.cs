using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Nexo.CLI;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Shared.Models;
using Nexo.Core.Application.Services;
using Nexo.Shared;
using System.Collections.Generic;

namespace Nexo.CLI.Tests
{
    /// <summary>
    /// Integration tests for the Nexo CLI application.
    /// </summary>
    public class IntegrationTests : IDisposable
    {
        private readonly IHost _host;

        public IntegrationTests()
        {
            // Build configuration for testing
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Cache:Backend"] = "inmemory",
                    ["Cache:DefaultTtlSeconds"] = "60"
                })
                .Build();

            // Build host for testing with mock providers
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddTestServices(configuration);
                })
                .Build();
        }

        [Fact]
        public async Task DependencyInjection_ShouldResolveAllServices()
        {
            using var scope = _host.Services.CreateScope();
            
            // Test that all core services can be resolved
            var cacheSettings = scope.ServiceProvider.GetRequiredService<Nexo.Shared.Models.CacheSettings>();
            var aiSettings = scope.ServiceProvider.GetRequiredService<AISettings>();
            var pipelineContext = scope.ServiceProvider.GetRequiredService<IPipelineContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationTests>>();

            Assert.NotNull(cacheSettings);
            Assert.NotNull(aiSettings);
            Assert.NotNull(pipelineContext);
            Assert.NotNull(logger);

            // Verify cache settings
            Assert.Equal("inmemory", cacheSettings.Backend);
            Assert.Equal(60, cacheSettings.DefaultTtlSeconds);
        }

        [Fact]
        public async Task CachingAsyncProcessor_ShouldWorkWithDI()
        {
            using var scope = _host.Services.CreateScope();
            var cachingProcessor = scope.ServiceProvider.GetRequiredService<CachingAsyncProcessor<ModelRequest, string, ModelResponse>>();
            
            Assert.NotNull(cachingProcessor);

            // Test basic functionality
            var request = new ModelRequest { Input = "Test input" };
            var response = await cachingProcessor.ProcessAsync(request);
            
            Assert.NotNull(response);
        }

        [Fact]
        public async Task PipelineContext_ShouldBeProperlyConfigured()
        {
            using var scope = _host.Services.CreateScope();
            var pipelineContext = scope.ServiceProvider.GetRequiredService<IPipelineContext>();
            
            Assert.NotNull(pipelineContext);
            Assert.NotNull(pipelineContext.Configuration);
            Assert.NotNull(pipelineContext.Logger);
        }

        [Fact]
        public async Task Constants_ShouldBeAccessible()
        {
            // Test that constants are properly defined
            Assert.Equal(10000, Constants.Timeouts.DefaultCommandTimeoutMs);
            Assert.Equal(300, Constants.Cache.DefaultTtlSeconds);
            Assert.Equal("inmemory", Constants.Defaults.DefaultCacheBackend);
            Assert.Equal("CACHE_BACKEND", Constants.EnvironmentVariables.CacheBackend);
        }

        [Fact]
        public async Task Configuration_ShouldUseConstants()
        {
            using var scope = _host.Services.CreateScope();
            var pipelineContext = scope.ServiceProvider.GetRequiredService<IPipelineContext>();
            
            // Verify that the configuration uses reasonable defaults
            Assert.Equal(Constants.Limits.DefaultMaxParallelExecutions, pipelineContext.Configuration.MaxParallelExecutions);
            Assert.Equal(30000, pipelineContext.Configuration.CommandTimeoutMs); // Default pipeline timeout
            Assert.Equal(Constants.Retry.DefaultMaxRetries, pipelineContext.Configuration.MaxRetries);
        }

        public void Dispose()
        {
            _host?.Dispose();
        }
    }
} 