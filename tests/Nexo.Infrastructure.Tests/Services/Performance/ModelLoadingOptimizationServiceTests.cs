using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Performance;
using Nexo.Infrastructure.Services.Performance;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.Performance
{
    /// <summary>
    /// Tests for model loading optimization service.
    /// Part of Phase 3.3 testing and validation.
    /// </summary>
    public class ModelLoadingOptimizationServiceTests
    {
        private readonly ModelLoadingOptimizationService _optimizationService;

        public ModelLoadingOptimizationServiceTests()
        {
            _optimizationService = new ModelLoadingOptimizationService();
        }

        [Fact]
        public async Task OptimizeModelLoadingAsync_WithValidInput_ReturnsOptimization()
        {
            // Arrange
            var modelName = "gpt-4";
            var context = new ModelLoadingContext
            {
                Priority = LoadingPriority.High,
                ExpectedUsage = 100,
                ResourceConstraints = new ResourceConstraints
                {
                    MemoryLimit = 2048,
                    CpuLimit = 80
                }
            };

            // Act
            var optimization = await _optimizationService.OptimizeModelLoadingAsync(modelName, context);

            // Assert
            Assert.NotNull(optimization);
            Assert.Equal(modelName, optimization.ModelName);
            Assert.True(optimization.OptimizedAt <= DateTimeOffset.UtcNow);
            Assert.NotNull(optimization.LoadingStrategy);
            Assert.NotNull(optimization.PreloadingRecommendations);
            Assert.NotNull(optimization.ResourceAllocation);
            Assert.NotNull(optimization.PerformancePredictions);
        }

        [Fact]
        public async Task OptimizeModelLoadingAsync_WithHighPriority_SelectsPreloadStrategy()
        {
            // Arrange
            var modelName = "gpt-4";
            var context = new ModelLoadingContext
            {
                Priority = LoadingPriority.High,
                ExpectedUsage = 100,
                ResourceConstraints = new ResourceConstraints
                {
                    MemoryLimit = 2048,
                    CpuLimit = 80
                }
            };

            // Act
            var optimization = await _optimizationService.OptimizeModelLoadingAsync(modelName, context);

            // Assert
            Assert.Equal(ModelLoadingStrategy.Preload, optimization.LoadingStrategy);
        }

        [Fact]
        public async Task OptimizeModelLoadingAsync_WithLowMemory_SelectsLazyLoadStrategy()
        {
            // Arrange
            var modelName = "gpt-4";
            var context = new ModelLoadingContext
            {
                Priority = LoadingPriority.Normal,
                ExpectedUsage = 100,
                ResourceConstraints = new ResourceConstraints
                {
                    MemoryLimit = 500, // Low memory
                    CpuLimit = 80
                }
            };

            // Act
            var optimization = await _optimizationService.OptimizeModelLoadingAsync(modelName, context);

            // Assert
            Assert.Equal(ModelLoadingStrategy.LazyLoad, optimization.LoadingStrategy);
        }

        [Fact]
        public async Task OptimizeModelLoadingAsync_WithHighUsage_SelectsCacheStrategy()
        {
            // Arrange
            var modelName = "gpt-4";
            var context = new ModelLoadingContext
            {
                Priority = LoadingPriority.Normal,
                ExpectedUsage = 200, // High usage
                ResourceConstraints = new ResourceConstraints
                {
                    MemoryLimit = 2048,
                    CpuLimit = 80
                }
            };

            // Act
            var optimization = await _optimizationService.OptimizeModelLoadingAsync(modelName, context);

            // Assert
            Assert.Equal(ModelLoadingStrategy.Cache, optimization.LoadingStrategy);
        }

        [Fact]
        public async Task PreloadModelsAsync_WithValidModels_ReturnsPreloadingResult()
        {
            // Arrange
            var modelNames = new[] { "gpt-4", "claude-3", "llama-2" };
            var context = new PreloadingContext
            {
                MaxConcurrentPreloads = 3,
                Timeout = TimeSpan.FromMinutes(5),
                ResourceConstraints = new ResourceConstraints
                {
                    MemoryLimit = 2048,
                    CpuLimit = 80
                }
            };

            // Act
            var result = await _optimizationService.PreloadModelsAsync(modelNames, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalModels);
            Assert.True(result.SuccessfullyPreloaded >= 0);
            Assert.True(result.FailedPreloads >= 0);
            Assert.Equal(3, result.Results.Count);
            Assert.True(result.TotalPreloadingTime >= TimeSpan.Zero);
        }

        [Fact]
        public async Task PreloadModelsAsync_WithEmptyList_ReturnsEmptyResult()
        {
            // Arrange
            var modelNames = new string[0];
            var context = new PreloadingContext();

            // Act
            var result = await _optimizationService.PreloadModelsAsync(modelNames, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalModels);
            Assert.Equal(0, result.SuccessfullyPreloaded);
            Assert.Equal(0, result.FailedPreloads);
            Assert.Empty(result.Results);
            Assert.Equal(TimeSpan.Zero, result.TotalPreloadingTime);
            Assert.Equal(TimeSpan.Zero, result.AveragePreloadingTime);
        }

        [Fact]
        public async Task MonitorModelPerformanceAsync_WithValidModel_ReturnsPerformanceReport()
        {
            // Arrange
            var modelName = "gpt-4";
            var monitoringDuration = TimeSpan.FromSeconds(1);

            // Act
            var report = await _optimizationService.MonitorModelPerformanceAsync(
                modelName, monitoringDuration);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(modelName, report.ModelName);
            Assert.True(report.MonitoringStartTime <= DateTimeOffset.UtcNow);
            Assert.True(report.MonitoringEndTime >= report.MonitoringStartTime);
            Assert.True(report.TotalRequests >= 0);
            Assert.True(report.SuccessfulRequests >= 0);
            Assert.True(report.FailedRequests >= 0);
            Assert.True(report.AverageResponseTime >= TimeSpan.Zero);
            Assert.True(report.PeakResponseTime >= TimeSpan.Zero);
            Assert.True(report.AverageMemoryUsage >= 0);
            Assert.True(report.PeakMemoryUsage >= 0);
            Assert.True(report.CpuUtilization >= 0);
            Assert.True(report.Throughput >= 0);
            Assert.True(report.ErrorRate >= 0);
        }

        [Fact]
        public async Task GetOptimizationRecommendationsAsync_ReturnsRecommendations()
        {
            // Act
            var recommendations = await _optimizationService.GetOptimizationRecommendationsAsync();

            // Assert
            Assert.NotNull(recommendations);
            Assert.NotEmpty(recommendations);
            
            var recommendation = recommendations.First();
            Assert.NotNull(recommendation.Title);
            Assert.NotNull(recommendation.Description);
            Assert.NotNull(recommendation.PotentialImprovement);
        }

        [Fact]
        public async Task OptimizeModelLoadingAsync_WithNullModelName_ThrowsArgumentNullException()
        {
            // Arrange
            var context = new ModelLoadingContext();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _optimizationService.OptimizeModelLoadingAsync(null!, context));
        }

        [Fact]
        public async Task OptimizeModelLoadingAsync_WithNullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var modelName = "gpt-4";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _optimizationService.OptimizeModelLoadingAsync(modelName, null!));
        }

        [Fact]
        public async Task PreloadModelsAsync_WithNullModelNames_ThrowsArgumentNullException()
        {
            // Arrange
            var context = new PreloadingContext();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _optimizationService.PreloadModelsAsync(null!, context));
        }

        [Fact]
        public async Task PreloadModelsAsync_WithNullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var modelNames = new[] { "gpt-4" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _optimizationService.PreloadModelsAsync(modelNames, null!));
        }

        [Fact]
        public async Task MonitorModelPerformanceAsync_WithNullModelName_ThrowsArgumentNullException()
        {
            // Arrange
            var monitoringDuration = TimeSpan.FromSeconds(1);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _optimizationService.MonitorModelPerformanceAsync(null!, monitoringDuration));
        }

        [Fact]
        public async Task MonitorModelPerformanceAsync_WithZeroDuration_ReturnsImmediateReport()
        {
            // Arrange
            var modelName = "gpt-4";
            var monitoringDuration = TimeSpan.Zero;

            // Act
            var report = await _optimizationService.MonitorModelPerformanceAsync(
                modelName, monitoringDuration);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(modelName, report.ModelName);
            Assert.True(report.MonitoringEndTime >= report.MonitoringStartTime);
        }

        [Fact]
        public async Task OptimizeModelLoadingAsync_WithDifferentPriorities_SelectsDifferentStrategies()
        {
            // Arrange
            var modelName = "gpt-4";
            var highPriorityContext = new ModelLoadingContext
            {
                Priority = LoadingPriority.High,
                ExpectedUsage = 50,
                ResourceConstraints = new ResourceConstraints
                {
                    MemoryLimit = 2048,
                    CpuLimit = 80
                }
            };

            var lowPriorityContext = new ModelLoadingContext
            {
                Priority = LoadingPriority.Low,
                ExpectedUsage = 50,
                ResourceConstraints = new ResourceConstraints
                {
                    MemoryLimit = 2048,
                    CpuLimit = 80
                }
            };

            // Act
            var highPriorityOptimization = await _optimizationService.OptimizeModelLoadingAsync(
                modelName, highPriorityContext);
            var lowPriorityOptimization = await _optimizationService.OptimizeModelLoadingAsync(
                modelName, lowPriorityContext);

            // Assert
            Assert.Equal(ModelLoadingStrategy.Preload, highPriorityOptimization.LoadingStrategy);
            Assert.Equal(ModelLoadingStrategy.OnDemand, lowPriorityOptimization.LoadingStrategy);
        }
    }
}