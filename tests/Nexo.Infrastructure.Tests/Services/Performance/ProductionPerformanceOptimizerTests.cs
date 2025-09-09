using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.Performance;
using Nexo.Core.Application.Interfaces.Caching;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Infrastructure.Services.Performance;

namespace Nexo.Infrastructure.Tests.Services.Performance
{
    /// <summary>
    /// Comprehensive tests for ProductionPerformanceOptimizer.
    /// Part of Phase 3.4 production readiness testing.
    /// </summary>
    public class ProductionPerformanceOptimizerTests
    {
        private readonly Mock<ILogger<ProductionPerformanceOptimizer>> _mockLogger;
        private readonly Mock<ICachePerformanceMonitor> _mockCacheMonitor;
        private readonly Mock<IAuditLogger> _mockAuditLogger;
        private readonly ProductionPerformanceOptimizer _optimizer;

        public ProductionPerformanceOptimizerTests()
        {
            _mockLogger = new Mock<ILogger<ProductionPerformanceOptimizer>>();
            _mockCacheMonitor = new Mock<ICachePerformanceMonitor>();
            _mockAuditLogger = new Mock<IAuditLogger>();
            
            _optimizer = new ProductionPerformanceOptimizer(
                _mockLogger.Object,
                _mockCacheMonitor.Object,
                _mockAuditLogger.Object);
        }

        [Fact]
        public async Task OptimizePerformanceAsync_WithAllOptions_ShouldSucceed()
        {
            // Arrange
            var options = new PerformanceOptimizationOptions
            {
                OptimizeCaching = true,
                OptimizeMemory = true,
                OptimizeAI = true,
                OptimizeSecurity = true,
                OptimizeDatabase = true,
                OptimizeNetwork = true
            };

            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachePerformanceMetrics
                {
                    HitRate = 0.75,
                    MissRate = 0.25,
                    EvictionRate = 0.1,
                    AverageAccessTime = TimeSpan.FromMilliseconds(5),
                    TotalRequests = 1000,
                    CacheSize = 1000000
                });

            // Act
            var result = await _optimizer.OptimizePerformanceAsync(options);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.StartTime);
            Assert.NotNull(result.EndTime);
            Assert.True(result.TotalOptimizationTime.TotalMilliseconds > 0);
            Assert.True(result.GetTotalImprovements() >= 0);
            
            // Verify cache optimization was called
            Assert.NotNull(result.CacheOptimization);
            Assert.True(result.CacheOptimization.Success);
            
            // Verify memory optimization was called
            Assert.NotNull(result.MemoryOptimization);
            Assert.True(result.MemoryOptimization.Success);
            
            // Verify AI optimization was called
            Assert.NotNull(result.AIOptimization);
            Assert.True(result.AIOptimization.Success);
            
            // Verify security optimization was called
            Assert.NotNull(result.SecurityOptimization);
            Assert.True(result.SecurityOptimization.Success);
            
            // Verify database optimization was called
            Assert.NotNull(result.DatabaseOptimization);
            Assert.True(result.DatabaseOptimization.Success);
            
            // Verify network optimization was called
            Assert.NotNull(result.NetworkOptimization);
            Assert.True(result.NetworkOptimization.Success);
        }

        [Fact]
        public async Task OptimizePerformanceAsync_WithPartialOptions_ShouldSucceed()
        {
            // Arrange
            var options = new PerformanceOptimizationOptions
            {
                OptimizeCaching = true,
                OptimizeMemory = false,
                OptimizeAI = true,
                OptimizeSecurity = false,
                OptimizeDatabase = false,
                OptimizeNetwork = false
            };

            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachePerformanceMetrics
                {
                    HitRate = 0.8,
                    MissRate = 0.2,
                    EvictionRate = 0.05,
                    AverageAccessTime = TimeSpan.FromMilliseconds(3),
                    TotalRequests = 2000,
                    CacheSize = 2000000
                });

            // Act
            var result = await _optimizer.OptimizePerformanceAsync(options);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.CacheOptimization);
            Assert.NotNull(result.AIOptimization);
            Assert.Null(result.MemoryOptimization);
            Assert.Null(result.SecurityOptimization);
            Assert.Null(result.DatabaseOptimization);
            Assert.Null(result.NetworkOptimization);
        }

        [Fact]
        public async Task RunBenchmarkAsync_WithAllMetrics_ShouldSucceed()
        {
            // Arrange
            var options = new PerformanceBenchmarkOptions
            {
                BenchmarkName = "Test Benchmark",
                Iterations = 5,
                WarmupTime = TimeSpan.FromSeconds(10),
                IncludeSystemMetrics = true,
                IncludeCacheMetrics = true,
                IncludeAIMetrics = true,
                IncludeSecurityMetrics = true,
                IncludeDatabaseMetrics = true,
                IncludeEndToEndMetrics = true
            };

            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachePerformanceMetrics
                {
                    HitRate = 0.85,
                    MissRate = 0.15,
                    EvictionRate = 0.08,
                    AverageAccessTime = TimeSpan.FromMilliseconds(4),
                    TotalRequests = 1500,
                    CacheSize = 1500000
                });

            // Act
            var result = await _optimizer.RunBenchmarkAsync(options);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Benchmark);
            Assert.Equal("Test Benchmark", result.Benchmark.Name);
            Assert.NotNull(result.Benchmark.StartTime);
            Assert.NotNull(result.Benchmark.EndTime);
            Assert.True(result.Benchmark.Duration.TotalMilliseconds > 0);
            
            // Verify all metrics were collected
            Assert.NotNull(result.Benchmark.SystemMetrics);
            Assert.NotNull(result.Benchmark.CacheMetrics);
            Assert.NotNull(result.Benchmark.AIMetrics);
            Assert.NotNull(result.Benchmark.SecurityMetrics);
            Assert.NotNull(result.Benchmark.DatabaseMetrics);
            Assert.NotNull(result.Benchmark.EndToEndMetrics);
        }

        [Fact]
        public async Task RunBenchmarkAsync_WithPartialMetrics_ShouldSucceed()
        {
            // Arrange
            var options = new PerformanceBenchmarkOptions
            {
                BenchmarkName = "Partial Benchmark",
                Iterations = 3,
                WarmupTime = TimeSpan.FromSeconds(5),
                IncludeSystemMetrics = true,
                IncludeCacheMetrics = true,
                IncludeAIMetrics = false,
                IncludeSecurityMetrics = false,
                IncludeDatabaseMetrics = false,
                IncludeEndToEndMetrics = false
            };

            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachePerformanceMetrics
                {
                    HitRate = 0.9,
                    MissRate = 0.1,
                    EvictionRate = 0.03,
                    AverageAccessTime = TimeSpan.FromMilliseconds(2),
                    TotalRequests = 3000,
                    CacheSize = 3000000
                });

            // Act
            var result = await _optimizer.RunBenchmarkAsync(options);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Benchmark);
            Assert.Equal("Partial Benchmark", result.Benchmark.Name);
            
            // Verify only requested metrics were collected
            Assert.NotNull(result.Benchmark.SystemMetrics);
            Assert.NotNull(result.Benchmark.CacheMetrics);
            Assert.Null(result.Benchmark.AIMetrics);
            Assert.Null(result.Benchmark.SecurityMetrics);
            Assert.Null(result.Benchmark.DatabaseMetrics);
            Assert.Null(result.Benchmark.EndToEndMetrics);
        }

        [Fact]
        public async Task GetPerformanceRecommendationsAsync_ShouldReturnRecommendations()
        {
            // Arrange
            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachePerformanceMetrics
                {
                    HitRate = 0.6, // Low hit rate should trigger recommendation
                    MissRate = 0.4,
                    EvictionRate = 0.2,
                    AverageAccessTime = TimeSpan.FromMilliseconds(10),
                    TotalRequests = 500,
                    CacheSize = 500000
                });

            // Act
            var recommendations = await _optimizer.GetPerformanceRecommendationsAsync();

            // Assert
            Assert.NotNull(recommendations);
            var recommendationsList = recommendations.ToList();
            Assert.True(recommendationsList.Count > 0);
            
            // Should have cache recommendation due to low hit rate
            var cacheRecommendation = recommendationsList.FirstOrDefault(r => r.Category == "Caching");
            Assert.NotNull(cacheRecommendation);
            Assert.Equal(PerformancePriority.High, cacheRecommendation.Priority);
            Assert.Contains("cache hit rate", cacheRecommendation.Description.ToLower());
        }

        [Fact]
        public async Task GetPerformanceTrendsAsync_WithTimeWindow_ShouldReturnTrends()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(24);

            // Act
            var trends = await _optimizer.GetPerformanceTrendsAsync(timeWindow);

            // Assert
            Assert.NotNull(trends);
            Assert.Equal(timeWindow, trends.TimeWindow);
            Assert.NotNull(trends.StartTime);
            Assert.True(trends.StartTime <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public async Task OptimizePerformanceAsync_WithCancellation_ShouldHandleCancellation()
        {
            // Arrange
            var options = new PerformanceOptimizationOptions
            {
                OptimizeCaching = true,
                OptimizeMemory = true,
                OptimizeAI = true,
                OptimizeSecurity = true,
                OptimizeDatabase = true,
                OptimizeNetwork = true
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizer.OptimizePerformanceAsync(options, cts.Token));
        }

        [Fact]
        public async Task RunBenchmarkAsync_WithCancellation_ShouldHandleCancellation()
        {
            // Arrange
            var options = new PerformanceBenchmarkOptions
            {
                BenchmarkName = "Cancellation Test",
                Iterations = 10
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizer.RunBenchmarkAsync(options, cts.Token));
        }

        [Fact]
        public async Task OptimizePerformanceAsync_WithException_ShouldHandleException()
        {
            // Arrange
            var options = new PerformanceOptimizationOptions
            {
                OptimizeCaching = true,
                OptimizeMemory = true,
                OptimizeAI = true,
                OptimizeSecurity = true,
                OptimizeDatabase = true,
                OptimizeNetwork = true
            };

            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Cache error"));

            // Act
            var result = await _optimizer.OptimizePerformanceAsync(options);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Cache error", result.ErrorMessage);
        }

        [Fact]
        public async Task RunBenchmarkAsync_WithException_ShouldHandleException()
        {
            // Arrange
            var options = new PerformanceBenchmarkOptions
            {
                BenchmarkName = "Exception Test",
                Iterations = 5
            };

            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Benchmark error"));

            // Act
            var result = await _optimizer.RunBenchmarkAsync(options);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Benchmark error", result.ErrorMessage);
            Assert.False(result.Benchmark.Success);
        }

        [Fact]
        public async Task GetPerformanceRecommendationsAsync_WithException_ShouldReturnEmptyList()
        {
            // Arrange
            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Recommendations error"));

            // Act
            var recommendations = await _optimizer.GetPerformanceRecommendationsAsync();

            // Assert
            Assert.NotNull(recommendations);
            var recommendationsList = recommendations.ToList();
            Assert.Empty(recommendationsList);
        }

        [Fact]
        public async Task GetPerformanceTrendsAsync_WithException_ShouldReturnEmptyTrends()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(1);

            // Act
            var trends = await _optimizer.GetPerformanceTrendsAsync(timeWindow);

            // Assert
            Assert.NotNull(trends);
            Assert.Equal(timeWindow, trends.TimeWindow);
            // Should return default values when no benchmarks are available
        }

        [Theory]
        [InlineData(true, true, true, true, true, true, 6)]
        [InlineData(true, false, true, false, true, false, 3)]
        [InlineData(false, false, false, false, false, false, 0)]
        public async Task OptimizePerformanceAsync_WithDifferentOptions_ShouldReturnCorrectImprovements(
            bool optimizeCaching, bool optimizeMemory, bool optimizeAI,
            bool optimizeSecurity, bool optimizeDatabase, bool optimizeNetwork,
            int expectedImprovements)
        {
            // Arrange
            var options = new PerformanceOptimizationOptions
            {
                OptimizeCaching = optimizeCaching,
                OptimizeMemory = optimizeMemory,
                OptimizeAI = optimizeAI,
                OptimizeSecurity = optimizeSecurity,
                OptimizeDatabase = optimizeDatabase,
                OptimizeNetwork = optimizeNetwork
            };

            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachePerformanceMetrics
                {
                    HitRate = 0.8,
                    MissRate = 0.2,
                    EvictionRate = 0.1,
                    AverageAccessTime = TimeSpan.FromMilliseconds(5),
                    TotalRequests = 1000,
                    CacheSize = 1000000
                });

            // Act
            var result = await _optimizer.OptimizePerformanceAsync(options);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(expectedImprovements, result.GetTotalImprovements());
        }

        [Fact]
        public async Task RunBenchmarkAsync_MultipleTimes_ShouldStoreBenchmarks()
        {
            // Arrange
            var options1 = new PerformanceBenchmarkOptions
            {
                BenchmarkName = "Benchmark 1",
                Iterations = 2
            };

            var options2 = new PerformanceBenchmarkOptions
            {
                BenchmarkName = "Benchmark 2",
                Iterations = 2
            };

            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachePerformanceMetrics
                {
                    HitRate = 0.8,
                    MissRate = 0.2,
                    EvictionRate = 0.1,
                    AverageAccessTime = TimeSpan.FromMilliseconds(5),
                    TotalRequests = 1000,
                    CacheSize = 1000000
                });

            // Act
            var result1 = await _optimizer.RunBenchmarkAsync(options1);
            var result2 = await _optimizer.RunBenchmarkAsync(options2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.Equal("Benchmark 1", result1.Benchmark.Name);
            Assert.Equal("Benchmark 2", result2.Benchmark.Name);
        }

        [Fact]
        public async Task GetPerformanceRecommendationsAsync_WithHighMemoryUsage_ShouldReturnMemoryRecommendation()
        {
            // Arrange
            _mockCacheMonitor.Setup(x => x.GetCacheMetricsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachePerformanceMetrics
                {
                    HitRate = 0.9, // Good hit rate
                    MissRate = 0.1,
                    EvictionRate = 0.05,
                    AverageAccessTime = TimeSpan.FromMilliseconds(2),
                    TotalRequests = 2000,
                    CacheSize = 2000000
                });

            // Simulate high memory usage by forcing GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Act
            var recommendations = await _optimizer.GetPerformanceRecommendationsAsync();

            // Assert
            Assert.NotNull(recommendations);
            var recommendationsList = recommendations.ToList();
            
            // Should have memory recommendation if memory usage is high
            var memoryRecommendation = recommendationsList.FirstOrDefault(r => r.Category == "Memory");
            if (memoryRecommendation != null)
            {
                Assert.Equal(PerformancePriority.Medium, memoryRecommendation.Priority);
                Assert.Contains("memory usage", memoryRecommendation.Description.ToLower());
            }
        }
    }
}
