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
    /// Cancellation test cases for PlatformPerformanceOptimizationService
    /// </summary>
    public partial class PlatformPerformanceOptimizationServiceTests
    {
        [Fact]
        public async Task OptimizePerformanceAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";
            var options = new PerformanceOptimizationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                service.OptimizePerformanceAsync(platform, options, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task OptimizeMemoryAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";
            var options = new PerformanceOptimizationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                service.OptimizeMemoryAsync(platform, options, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task OptimizeCPUAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";
            var options = new PerformanceOptimizationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                service.OptimizeCPUAsync(platform, options, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task OptimizeGPUAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";
            var options = new PerformanceOptimizationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                service.OptimizeGPUAsync(platform, options, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task OptimizeNetworkAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";
            var options = new PerformanceOptimizationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                service.OptimizeNetworkAsync(platform, options, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task OptimizeBatteryAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";
            var options = new PerformanceOptimizationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                service.OptimizeBatteryAsync(platform, options, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task OptimizeStorageAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";
            var options = new PerformanceOptimizationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                service.OptimizeStorageAsync(platform, options, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task OptimizeConsistencyAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var service = new PlatformPerformanceOptimizationService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";
            var options = new PerformanceOptimizationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                service.OptimizeConsistencyAsync(platform, options, cancellationTokenSource.Token));
        }
    }
}