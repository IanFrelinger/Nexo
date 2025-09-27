using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Platform;
using Nexo.Infrastructure.Services.Platform;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.Platform
{
    /// <summary>
    /// Error handling test cases for PlatformPerformanceOptimizationService
    /// </summary>
    public partial class PlatformPerformanceOptimizationServiceTests
    {
        [Fact]
        public async Task OptimizePerformanceAsync_WithNullPlatform_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var options = new PerformanceOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.OptimizePerformanceAsync(null, options));
        }

        [Fact]
        public async Task OptimizePerformanceAsync_WithEmptyPlatform_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var options = new PerformanceOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.OptimizePerformanceAsync("", options));
        }

        [Fact]
        public async Task OptimizePerformanceAsync_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.OptimizePerformanceAsync("iOS", null));
        }

        [Fact]
        public async Task OptimizeMemoryAsync_WithNullPlatform_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var options = new PerformanceOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.OptimizeMemoryAsync(null, options));
        }

        [Fact]
        public async Task OptimizeCPUAsync_WithNullPlatform_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var options = new PerformanceOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.OptimizeCPUAsync(null, options));
        }

        [Fact]
        public async Task OptimizeGPUAsync_WithNullPlatform_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var options = new PerformanceOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.OptimizeGPUAsync(null, options));
        }

        [Fact]
        public async Task OptimizeNetworkAsync_WithNullPlatform_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var options = new PerformanceOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.OptimizeNetworkAsync(null, options));
        }

        [Fact]
        public async Task OptimizeBatteryAsync_WithNullPlatform_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var options = new PerformanceOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.OptimizeBatteryAsync(null, options));
        }

        [Fact]
        public async Task OptimizeStorageAsync_WithNullPlatform_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var options = new PerformanceOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.OptimizeStorageAsync(null, options));
        }

        [Fact]
        public async Task OptimizeConsistencyAsync_WithNullPlatform_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var options = new PerformanceOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.OptimizeConsistencyAsync(null, options));
        }
    }
}