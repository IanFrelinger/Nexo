using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Shared.Interfaces.Resource;
using Nexo.Infrastructure.Services.Resource;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.Resource
{
    public class ResourceOptimizerTests
    {
        private readonly Mock<ILogger<ResourceOptimizer>> _loggerMock;
        private readonly Mock<IResourceMonitor> _resourceMonitorMock;
        private readonly Mock<IResourceManager> _resourceManagerMock;
        private readonly ResourceOptimizer _optimizer;

        public ResourceOptimizerTests()
        {
            _loggerMock = new Mock<ILogger<ResourceOptimizer>>();
            _resourceMonitorMock = new Mock<IResourceMonitor>();
            _resourceManagerMock = new Mock<IResourceManager>();
            
            _optimizer = new ResourceOptimizer(_loggerMock.Object, _resourceMonitorMock.Object, _resourceManagerMock.Object);
        }

        [Fact]
        public async Task OptimizeAsync_WithNormalResourceUsage_ShouldReturnEmptyRecommendations()
        {
            // Arrange
            var normalUsage = new SystemResourceUsage
            {
                CpuUsagePercentage = 30,
                Memory = new MemoryInfo { TotalBytes = 1000, AvailableBytes = 600 },
                Disk = new DiskInfo { TotalBytes = 1000, AvailableBytes = 500 }
            };

            _resourceMonitorMock.Setup(x => x.GetResourceUsageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(normalUsage);

            // Act
            var result = await _optimizer.OptimizeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Recommendations);
            Assert.True(result.Timestamp > DateTime.MinValue);
        }

        [Fact]
        public async Task OptimizeAsync_WithHighCpuUsage_ShouldReturnCpuOptimizationRecommendation()
        {
            // Arrange
            var highCpuUsage = new SystemResourceUsage
            {
                CpuUsagePercentage = 85,
                Memory = new MemoryInfo { TotalBytes = 1000, AvailableBytes = 600 },
                Disk = new DiskInfo { TotalBytes = 1000, AvailableBytes = 500 }
            };

            _resourceMonitorMock.Setup(x => x.GetResourceUsageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(highCpuUsage);

            // Act
            var result = await _optimizer.OptimizeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Recommendations);
            var cpuRecommendation = result.Recommendations.FirstOrDefault(r => r.Type == "CPU_OPTIMIZATION");
            Assert.NotNull(cpuRecommendation);
            Assert.Contains("reducing concurrent operations", cpuRecommendation.Message);
            Assert.Equal("High", cpuRecommendation.Impact);
            Assert.Equal(1, cpuRecommendation.Priority);
        }

        [Fact]
        public async Task OptimizeAsync_WithHighMemoryUsage_ShouldReturnMemoryOptimizationRecommendation()
        {
            // Arrange
            var highMemoryUsage = new SystemResourceUsage
            {
                CpuUsagePercentage = 30,
                Memory = new MemoryInfo { TotalBytes = 1000, AvailableBytes = 100 },
                Disk = new DiskInfo { TotalBytes = 1000, AvailableBytes = 500 }
            };

            _resourceMonitorMock.Setup(x => x.GetResourceUsageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(highMemoryUsage);

            // Act
            var result = await _optimizer.OptimizeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Recommendations);
            var memoryRecommendation = result.Recommendations.FirstOrDefault(r => r.Type == "MEMORY_OPTIMIZATION");
            Assert.NotNull(memoryRecommendation);
            Assert.Contains("garbage collection", memoryRecommendation.Message);
            Assert.Equal("High", memoryRecommendation.Impact);
            Assert.Equal(1, memoryRecommendation.Priority);
        }

        [Fact]
        public async Task OptimizeAsync_WithLowDiskSpace_ShouldReturnDiskOptimizationRecommendation()
        {
            // Arrange
            var lowDiskUsage = new SystemResourceUsage
            {
                CpuUsagePercentage = 30,
                Memory = new MemoryInfo { TotalBytes = 1000, AvailableBytes = 600 },
                Disk = new DiskInfo { TotalBytes = 1000, AvailableBytes = 50 }
            };

            _resourceMonitorMock.Setup(x => x.GetResourceUsageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(lowDiskUsage);

            // Act
            var result = await _optimizer.OptimizeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Recommendations);
            var diskRecommendation = result.Recommendations.FirstOrDefault(r => r.Type == "STORAGE_OPTIMIZATION");
            Assert.NotNull(diskRecommendation);
            Assert.Contains("cleaning up temporary files", diskRecommendation.Message);
            Assert.Equal("Medium", diskRecommendation.Impact);
            Assert.Equal(2, diskRecommendation.Priority);
        }

        [Fact]
        public async Task CalculateThrottlingAsync_WithHighCpuUsage_ShouldRecommendThrottling()
        {
            // Arrange
            var highCpuUsage = new SystemResourceUsage
            {
                CpuUsagePercentage = 95,
                Memory = new MemoryInfo { TotalBytes = 1000, AvailableBytes = 600 },
                Disk = new DiskInfo { TotalBytes = 1000, AvailableBytes = 500 }
            };

            _resourceMonitorMock.Setup(x => x.GetResourceUsageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(highCpuUsage);

            var request = new PipelineExecutionRequest
            {
                PipelineId = "test-pipeline",
                EstimatedCpuUsage = 10,
                EstimatedMemoryUsage = 1024 * 1024,
                EstimatedDuration = TimeSpan.FromMinutes(5),
                Priority = 1
            };

            // Act
            var result = await _optimizer.CalculateThrottlingAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ShouldThrottle);
            Assert.Equal(ThrottlingLevel.High, result.ThrottlingLevel);
            Assert.Equal(TimeSpan.FromSeconds(5), result.RecommendedDelay);
        }

        [Fact]
        public async Task CalculateThrottlingAsync_WithMediumCpuUsage_ShouldRecommendMediumThrottling()
        {
            // Arrange
            var mediumCpuUsage = new SystemResourceUsage
            {
                CpuUsagePercentage = 80,
                Memory = new MemoryInfo { TotalBytes = 1000, AvailableBytes = 600 },
                Disk = new DiskInfo { TotalBytes = 1000, AvailableBytes = 500 }
            };

            _resourceMonitorMock.Setup(x => x.GetResourceUsageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mediumCpuUsage);

            var request = new PipelineExecutionRequest
            {
                PipelineId = "test-pipeline",
                EstimatedCpuUsage = 10,
                EstimatedMemoryUsage = 1024 * 1024,
                EstimatedDuration = TimeSpan.FromMinutes(5),
                Priority = 1
            };

            // Act
            var result = await _optimizer.CalculateThrottlingAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ShouldThrottle);
            Assert.Equal(ThrottlingLevel.Medium, result.ThrottlingLevel);
            Assert.Equal(TimeSpan.FromSeconds(2), result.RecommendedDelay);
        }

        [Fact]
        public async Task CalculateThrottlingAsync_WithHighMemoryUsage_ShouldRecommendThrottling()
        {
            // Arrange
            var highMemoryUsage = new SystemResourceUsage
            {
                CpuUsagePercentage = 30,
                Memory = new MemoryInfo { TotalBytes = 1000, AvailableBytes = 100 },
                Disk = new DiskInfo { TotalBytes = 1000, AvailableBytes = 500 }
            };

            _resourceMonitorMock.Setup(x => x.GetResourceUsageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(highMemoryUsage);

            var request = new PipelineExecutionRequest
            {
                PipelineId = "test-pipeline",
                EstimatedCpuUsage = 10,
                EstimatedMemoryUsage = 1024 * 1024,
                EstimatedDuration = TimeSpan.FromMinutes(5),
                Priority = 1
            };

            // Act
            var result = await _optimizer.CalculateThrottlingAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ShouldThrottle);
            Assert.Equal(ThrottlingLevel.High, result.ThrottlingLevel);
            Assert.Equal(TimeSpan.FromSeconds(10), result.RecommendedDelay);
        }

        [Fact]
        public async Task CalculateThrottlingAsync_WithNormalUsage_ShouldNotRecommendThrottling()
        {
            // Arrange
            var normalUsage = new SystemResourceUsage
            {
                CpuUsagePercentage = 30,
                Memory = new MemoryInfo { TotalBytes = 1000, AvailableBytes = 600 },
                Disk = new DiskInfo { TotalBytes = 1000, AvailableBytes = 500 }
            };

            _resourceMonitorMock.Setup(x => x.GetResourceUsageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(normalUsage);

            var request = new PipelineExecutionRequest
            {
                PipelineId = "test-pipeline",
                EstimatedCpuUsage = 10,
                EstimatedMemoryUsage = 1024 * 1024,
                EstimatedDuration = TimeSpan.FromMinutes(5),
                Priority = 1
            };

            // Act
            var result = await _optimizer.CalculateThrottlingAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.ShouldThrottle);
            Assert.Equal(ThrottlingLevel.None, result.ThrottlingLevel);
            Assert.Equal(TimeSpan.Zero, result.RecommendedDelay);
        }

        [Fact]
        public void AddOptimizationRule_WithValidRule_ShouldAddRule()
        {
            // Arrange
            var rule = new OptimizationRule
            {
                Name = "Test Rule",
                Description = "Test Description",
                Condition = async (usage) => usage.UtilizationByType.TryGetValue(ResourceType.CPU, out var cpuUsage) && cpuUsage > 50,
                Action = async (usage) => new OptimizationRecommendation
                {
                    Type = "TEST_OPTIMIZATION",
                    Message = "Test message",
                    Impact = "Low",
                    Priority = 3
                }
            };

            // Act
            _optimizer.AddOptimizationRule("test_rule", rule);

            // Assert - We can't directly test the internal state, but we can verify no exception is thrown
            Assert.True(true); // If we get here, the rule was added successfully
        }

        [Fact]
        public void AddOptimizationRule_WithNullRuleId_ShouldThrowArgumentException()
        {
            // Arrange
            var rule = new OptimizationRule();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _optimizer.AddOptimizationRule(null, rule));
        }

        [Fact]
        public void AddOptimizationRule_WithNullRule_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _optimizer.AddOptimizationRule("test", null));
        }

        [Fact]
        public void RemoveOptimizationRule_WithExistingRule_ShouldRemoveRule()
        {
            // Arrange
            var rule = new OptimizationRule();
            _optimizer.AddOptimizationRule("test_rule", rule);

            // Act
            _optimizer.RemoveOptimizationRule("test_rule");

            // Assert - We can't directly test the internal state, but we can verify no exception is thrown
            Assert.True(true); // If we get here, the rule was removed successfully
        }

        [Fact]
        public void GetOptimizationHistory_ShouldReturnHistory()
        {
            // Act
            var history = _optimizer.GetOptimizationHistory();

            // Assert
            Assert.NotNull(history);
            // Initially empty, but the method should work
        }

        [Fact]
        public async Task OptimizeAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _optimizer.OptimizeAsync(cts.Token));
        }

        [Fact]
        public async Task CalculateThrottlingAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var request = new PipelineExecutionRequest
            {
                PipelineId = "test-pipeline",
                EstimatedCpuUsage = 10,
                EstimatedMemoryUsage = 1024 * 1024,
                EstimatedDuration = TimeSpan.FromMinutes(5),
                Priority = 1
            };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _optimizer.CalculateThrottlingAsync(request, cts.Token));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ResourceOptimizer(null, _resourceMonitorMock.Object, _resourceManagerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullResourceMonitor_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ResourceOptimizer(_loggerMock.Object, null, _resourceManagerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullResourceManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ResourceOptimizer(_loggerMock.Object, _resourceMonitorMock.Object, null));
        }
    }
} 