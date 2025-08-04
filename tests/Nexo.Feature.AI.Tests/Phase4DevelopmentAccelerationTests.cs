using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;
using Nexo.Feature.Template.Interfaces;
using Nexo.Feature.Template.Services;
using Nexo.Core.Application.Interfaces;
using Nexo.Infrastructure.Services.AI;
using Nexo.Shared.Interfaces;
using Nexo.Shared.Interfaces.Resource;
using Nexo.Shared.Models;

namespace Nexo.Feature.AI.Tests
{
    /// <summary>
    /// Integration tests for Phase 4: Development Acceleration features.
    /// </summary>
    public class Phase4DevelopmentAccelerationTests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;

        public Phase4DevelopmentAccelerationTests()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

            // Add AI services
            services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();
            services.AddSingleton<IModelOrchestrator, MockCoreModelOrchestrator>();
            
            // Add model providers
            services.AddSingleton<MockModelProvider>();
            services.AddSingleton<IModelProvider, MockModelProvider>(sp => sp.GetRequiredService<MockModelProvider>());

            // Add AI-enhanced services
            services.AddTransient<IDevelopmentAccelerator, DevelopmentAccelerator>();
            services.AddTransient<ITemplateService, MockTemplateService>();
            services.AddTransient<IIntelligentTemplateService, IntelligentTemplateService>();

            // Add Phase 4 services
            services.AddTransient<IAdvancedCachingService, AdvancedCachingService>();
            services.AddTransient<IParallelProcessingOptimizer, ParallelProcessingOptimizer>();

            // Add infrastructure services
            services.AddSingleton<Nexo.Feature.AI.Interfaces.ICacheStrategy<string, ModelResponse>, MockCacheStrategy>();
            services.AddSingleton<Nexo.Shared.Interfaces.ISemanticCacheKeyGenerator, MockSemanticCacheKeyGenerator>();
            services.AddSingleton<Nexo.Shared.Interfaces.Resource.IResourceManager, MockResourceManager>();
            services.AddSingleton<Nexo.Shared.Interfaces.Resource.IResourceMonitor, MockResourceMonitor>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task AdvancedCachingService_GetOrSetAsync_ReturnsCachedResponse()
        {
            // Arrange
            var cachingService = _serviceProvider.GetRequiredService<IAdvancedCachingService>();
            var request = new ModelRequest { Input = "Test request", MaxTokens = 100 };

            // Act
            var response1 = await cachingService.GetOrSetAsync(request, async () => new ModelResponse { Content = "First response" });
            var response2 = await cachingService.GetOrSetAsync(request, async () => new ModelResponse { Content = "Second response" });

            // Assert
            Assert.NotNull(response1);
            Assert.NotNull(response2);
            Assert.Equal(response1.Content, response2.Content);
        }

        [Fact]
        public async Task AdvancedCachingService_GetSimilarResponsesAsync_ReturnsSimilarResponses()
        {
            // Arrange
            var cachingService = _serviceProvider.GetRequiredService<IAdvancedCachingService>();
            var request1 = new ModelRequest { Input = "Generate a user controller", MaxTokens = 100 };
            var request2 = new ModelRequest { Input = "Create a user controller", MaxTokens = 100 };

            // Cache first request
            await cachingService.GetOrSetAsync(request1, async () => new ModelResponse { Content = "User controller code" });

            // Act
            var similarResponses = await cachingService.GetSimilarResponsesAsync(request2, 0.5);

            // Assert
            Assert.NotNull(similarResponses);
            Assert.NotEmpty(similarResponses);
        }

        [Fact]
        public async Task AdvancedCachingService_GetStatisticsAsync_ReturnsValidStatistics()
        {
            // Arrange
            var cachingService = _serviceProvider.GetRequiredService<IAdvancedCachingService>();
            var request = new ModelRequest { Input = "Test request", MaxTokens = 100 };

            // Cache some requests
            await cachingService.GetOrSetAsync(request, async () => new ModelResponse { Content = "Test response" });

            // Act
            var statistics = await cachingService.GetStatisticsAsync();

            // Assert
            Assert.NotNull(statistics);
            Assert.True(statistics.TotalEntries > 0);
            Assert.True(statistics.AverageResponseSize > 0);
        }

        [Fact]
        public async Task ParallelProcessingOptimizer_DetermineOptimalStrategyAsync_ReturnsValidStrategy()
        {
            // Arrange
            var optimizer = _serviceProvider.GetRequiredService<IParallelProcessingOptimizer>();
            var requests = new List<ModelRequest>
            {
                new ModelRequest { Input = "Request 1", MaxTokens = 100 },
                new ModelRequest { Input = "Request 2", MaxTokens = 200 },
                new ModelRequest { Input = "Request 3", MaxTokens = 150 }
            };

            // Act
            var strategy = await optimizer.DetermineOptimalStrategyAsync(requests);

            // Assert
            Assert.NotNull(strategy);
            Assert.True(strategy.MaxParallelism > 0);
            Assert.True(strategy.BatchSize > 0);
            Assert.NotNull(strategy.ProcessingOrder);
            Assert.Equal(requests.Count, strategy.ProcessingOrder.Count);
        }

        [Fact]
        public async Task ParallelProcessingOptimizer_ProcessInParallelAsync_ProcessesAllRequests()
        {
            // Arrange
            var optimizer = _serviceProvider.GetRequiredService<IParallelProcessingOptimizer>();
            var requests = new List<ModelRequest>
            {
                new ModelRequest { Input = "Request 1", MaxTokens = 100 },
                new ModelRequest { Input = "Request 2", MaxTokens = 200 },
                new ModelRequest { Input = "Request 3", MaxTokens = 150 }
            };

            var strategy = await optimizer.DetermineOptimalStrategyAsync(requests);

            // Act
            var responses = await optimizer.ProcessInParallelAsync(
                requests,
                strategy,
                async (request, ct) => new ModelResponse { Content = $"Response for {request.Input}" });

            // Assert
            Assert.NotNull(responses);
            Assert.Equal(requests.Count, responses.Count());
            Assert.All(responses, response => Assert.NotNull(response.Content));
        }

        [Fact]
        public async Task ParallelProcessingOptimizer_GetPerformanceMetricsAsync_ReturnsValidMetrics()
        {
            // Arrange
            var optimizer = _serviceProvider.GetRequiredService<IParallelProcessingOptimizer>();
            var requests = new List<ModelRequest>
            {
                new ModelRequest { Input = "Request 1", MaxTokens = 100 },
                new ModelRequest { Input = "Request 2", MaxTokens = 200 }
            };

            var strategy = await optimizer.DetermineOptimalStrategyAsync(requests);
            await optimizer.ProcessInParallelAsync(
                requests,
                strategy,
                async (request, ct) => new ModelResponse { Content = $"Response for {request.Input}" });

            // Act
            var metrics = await optimizer.GetPerformanceMetricsAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalRequestsProcessed > 0);
            Assert.True(metrics.AverageProcessingTime > 0);
            Assert.True(metrics.SuccessRate >= 0 && metrics.SuccessRate <= 1);
        }

        [Fact]
        public async Task DevelopmentAccelerator_WithAdvancedCaching_ProvidesCachedSuggestions()
        {
            // Arrange
            var accelerator = _serviceProvider.GetRequiredService<IDevelopmentAccelerator>();
            var testCode = @"
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}";

            // Act
            var suggestions1 = await accelerator.SuggestCodeAsync(testCode);
            var suggestions2 = await accelerator.SuggestCodeAsync(testCode);

            // Assert
            Assert.NotNull(suggestions1);
            Assert.NotNull(suggestions2);
            Assert.NotEmpty(suggestions1);
            Assert.NotEmpty(suggestions2);
        }

        [Fact]
        public async Task IntelligentTemplateService_WithAdvancedCaching_GeneratesCachedTemplates()
        {
            // Arrange
            var templateService = _serviceProvider.GetRequiredService<IIntelligentTemplateService>();
            var description = "A REST API controller for user management";

            // Act
            var template1 = await templateService.GenerateTemplateAsync(description);
            var template2 = await templateService.GenerateTemplateAsync(description);

            // Assert
            Assert.NotNull(template1);
            Assert.NotNull(template2);
            Assert.NotEmpty(template1);
            Assert.NotEmpty(template2);
        }

        [Fact]
        public async Task Phase4Integration_CompleteWorkflow_WorksEndToEnd()
        {
            // Arrange
            var accelerator = _serviceProvider.GetRequiredService<IDevelopmentAccelerator>();
            var optimizer = _serviceProvider.GetRequiredService<IParallelProcessingOptimizer>();
            var cachingService = _serviceProvider.GetRequiredService<IAdvancedCachingService>();

            var requests = new List<ModelRequest>
            {
                new ModelRequest { Input = "Generate a user service", MaxTokens = 200, Metadata = new Dictionary<string, object> { ["type"] = "code_generation" } },
                new ModelRequest { Input = "Create unit tests for user service", MaxTokens = 300, Metadata = new Dictionary<string, object> { ["type"] = "test_generation" } },
                new ModelRequest { Input = "Optimize user service performance", MaxTokens = 250, Metadata = new Dictionary<string, object> { ["type"] = "optimization" } }
            };

            // Act - Determine optimal strategy
            var strategy = await optimizer.DetermineOptimalStrategyAsync(requests);

            // Act - Process in parallel with caching
            var responses = await optimizer.ProcessInParallelAsync(
                requests,
                strategy,
                async (request, ct) =>
                {
                    return await cachingService.GetOrSetAsync(
                        request,
                        async () => new ModelResponse 
                        { 
                            Content = $"Processed: {request.Input}"
                        },
                        ct);
                });

            // Act - Get performance metrics
            var metrics = await optimizer.GetPerformanceMetricsAsync();

            // Assert
            Assert.NotNull(strategy);
            Assert.True(strategy.MaxParallelism > 0);
            Assert.Equal(requests.Count, responses.Count());
            Assert.All(responses, response => Assert.NotNull(response.Content));
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalRequestsProcessed > 0);
        }

        public void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    // Mock implementations for testing

    public class MockCacheStrategy : Nexo.Feature.AI.Interfaces.ICacheStrategy<string, ModelResponse>
    {
        private readonly Dictionary<string, ModelResponse> _cache = new Dictionary<string, ModelResponse>();

        public Task<ModelResponse> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cache.TryGetValue(key, out var value) ? value : null);
        }

        public Task SetAsync(string key, ModelResponse value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            _cache[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _cache.Clear();
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
    }

    public class MockSemanticCacheKeyGenerator : Nexo.Shared.Interfaces.ISemanticCacheKeyGenerator
    {
        public string Generate(string input, IDictionary<string, object> metadata = null)
        {
            return $"mock_key_{input?.GetHashCode() ?? 0}";
        }
    }

    public class MockResourceManager : Nexo.Shared.Interfaces.Resource.IResourceManager
    {
        public Task<ResourceLimits> GetLimitsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResourceLimits
            {
                MaximumByType = new Dictionary<Nexo.Shared.Interfaces.Resource.ResourceType, long>
                {
                    { Nexo.Shared.Interfaces.Resource.ResourceType.CPU, 80 },
                    { Nexo.Shared.Interfaces.Resource.ResourceType.Memory, 1024L * 1024L * 1024L }, // 1GB
                    { Nexo.Shared.Interfaces.Resource.ResourceType.Network, 100L * 1024L * 1024L } // 100MB
                }
            });
        }

        public Task<Nexo.Shared.Interfaces.Resource.ResourceAllocationResult> AllocateAsync(Nexo.Shared.Interfaces.Resource.ResourceAllocationRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Nexo.Shared.Interfaces.Resource.ResourceAllocationResult
            {
                IsSuccessful = true,
                AllocationId = Guid.NewGuid().ToString(),
                AllocatedAmount = request.Amount,
                ProviderId = "MockProvider",
                AllocatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(request.Duration)
            });
        }

        public Task ReleaseAsync(string allocationId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<Nexo.Shared.Interfaces.Resource.ResourceUsage> GetUsageAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Nexo.Shared.Interfaces.Resource.ResourceUsage
            {
                AllocatedByType = new Dictionary<Nexo.Shared.Interfaces.Resource.ResourceType, long>(),
                AvailableByType = new Dictionary<Nexo.Shared.Interfaces.Resource.ResourceType, long>(),
                UtilizationByType = new Dictionary<Nexo.Shared.Interfaces.Resource.ResourceType, double>(),
                ActiveAllocations = new List<Nexo.Shared.Interfaces.Resource.ResourceAllocation>()
            });
        }

        public Task<Nexo.Shared.Interfaces.Resource.ResourceMonitoringInfo> MonitorAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Nexo.Shared.Interfaces.Resource.ResourceMonitoringInfo
            {
                Alerts = new List<Nexo.Shared.Interfaces.Resource.ResourceAlert>(),
                MetricsByType = new Dictionary<Nexo.Shared.Interfaces.Resource.ResourceType, Nexo.Shared.Interfaces.Resource.ResourceMetrics>(),
                HealthStatus = new Nexo.Shared.Interfaces.Resource.ResourceHealthStatus()
            });
        }

        public Task<Nexo.Shared.Interfaces.Resource.ResourceOptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Nexo.Shared.Interfaces.Resource.ResourceOptimizationResult
            {
                IsSuccessful = true,
                Recommendations = new List<Nexo.Shared.Interfaces.Resource.ResourceOptimizationRecommendation>(),
                ExpectedImprovements = new Dictionary<Nexo.Shared.Interfaces.Resource.ResourceType, double>()
            });
        }

        public Task RegisterProviderAsync(Nexo.Shared.Interfaces.Resource.IResourceProvider provider, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UnregisterProviderAsync(string providerId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class MockResourceMonitor : Nexo.Shared.Interfaces.Resource.IResourceMonitor
    {
        public Task<SystemResourceUsage> GetResourceUsageAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SystemResourceUsage
            {
                CpuUsagePercentage = 50.0,
                Memory = new Nexo.Shared.Interfaces.Resource.MemoryInfo
                {
                    TotalBytes = 8L * 1024L * 1024L * 1024L, // 8GB
                    AvailableBytes = 4L * 1024L * 1024L * 1024L // 4GB
                },
                Disk = new Nexo.Shared.Interfaces.Resource.DiskInfo
                {
                    TotalBytes = 100L * 1024L * 1024L * 1024L, // 100GB
                    AvailableBytes = 60L * 1024L * 1024L * 1024L // 60GB
                }
            });
        }

        public Task<double> GetCpuUsageAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(50.0);
        }

        public Task<Nexo.Shared.Interfaces.Resource.MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Nexo.Shared.Interfaces.Resource.MemoryInfo
            {
                TotalBytes = 8L * 1024L * 1024L * 1024L, // 8GB
                AvailableBytes = 4L * 1024L * 1024L * 1024L // 4GB
            });
        }

        public Task<Nexo.Shared.Interfaces.Resource.DiskInfo> GetDiskInfoAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Nexo.Shared.Interfaces.Resource.DiskInfo
            {
                TotalBytes = 100L * 1024L * 1024L * 1024L, // 100GB
                AvailableBytes = 60L * 1024L * 1024L * 1024L, // 60GB
                Path = path
            });
        }
    }


} 