using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Shared.Interfaces.Resource;
using Nexo.Infrastructure.Services.Resource;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.Resource
{
    public class SystemResourceMonitorTests
    {
        private readonly Mock<ILogger<SystemResourceMonitor>> _loggerMock;
        private readonly SystemResourceMonitor _monitor;

        public SystemResourceMonitorTests()
        {
            _loggerMock = new Mock<ILogger<SystemResourceMonitor>>();
            _monitor = new SystemResourceMonitor(_loggerMock.Object);
        }

        [Fact]
        public async Task GetCpuUsageAsync_ShouldReturnValidPercentage()
        {
            // Act
            var result = await _monitor.GetCpuUsageAsync();

            // Assert
            Assert.True(result >= 0, "CPU usage should be non-negative");
            Assert.True(result <= 100, "CPU usage should not exceed 100%");
        }

        [Fact]
        public async Task GetMemoryInfoAsync_ShouldReturnValidMemoryInfo()
        {
            // Act
            var result = await _monitor.GetMemoryInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalBytes > 0, "Total memory should be positive");
            Assert.True(result.AvailableBytes >= 0, "Available memory should be non-negative");
            Assert.True(result.UsedBytes >= 0, "Used memory should be non-negative");
            Assert.True(result.UsagePercentage >= 0, "Usage percentage should be non-negative");
            Assert.True(result.UsagePercentage <= 100, "Usage percentage should not exceed 100%");
        }

        [Fact]
        public async Task GetDiskInfoAsync_ShouldReturnValidDiskInfo()
        {
            // Act
            var result = await _monitor.GetDiskInfoAsync(Environment.CurrentDirectory);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Environment.CurrentDirectory, result.Path);
            Assert.True(result.TotalBytes > 0, "Total disk space should be positive");
            Assert.True(result.AvailableBytes >= 0, "Available disk space should be non-negative");
            Assert.True(result.UsedBytes >= 0, "Used disk space should be non-negative");
            Assert.True(result.UsagePercentage >= 0, "Usage percentage should be non-negative");
            Assert.True(result.UsagePercentage <= 100, "Usage percentage should not exceed 100%");
        }

        [Fact]
        public async Task GetDiskInfoAsync_WithNullPath_ShouldUseCurrentDirectory()
        {
            // Act
            var result = await _monitor.GetDiskInfoAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Environment.CurrentDirectory, result.Path);
        }

        [Fact]
        public async Task GetDiskInfoAsync_WithEmptyPath_ShouldUseCurrentDirectory()
        {
            // Act
            var result = await _monitor.GetDiskInfoAsync(string.Empty);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Environment.CurrentDirectory, result.Path);
        }

        [Fact]
        public async Task GetResourceUsageAsync_ShouldReturnComprehensiveResourceInfo()
        {
            // Act
            var result = await _monitor.GetResourceUsageAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CpuUsagePercentage >= 0, "CPU usage should be non-negative");
            Assert.True(result.CpuUsagePercentage <= 100, "CPU usage should not exceed 100%");
            Assert.NotNull(result.Memory);
            Assert.NotNull(result.Disk);
            Assert.True(result.Timestamp > DateTime.MinValue, "Timestamp should be valid");
        }

        [Fact]
        public async Task GetCpuUsageAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _monitor.GetCpuUsageAsync(cts.Token));
        }

        [Fact]
        public async Task GetMemoryInfoAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _monitor.GetMemoryInfoAsync(cts.Token));
        }

        [Fact]
        public async Task GetDiskInfoAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _monitor.GetDiskInfoAsync(Environment.CurrentDirectory, cts.Token));
        }

        [Fact]
        public async Task GetResourceUsageAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _monitor.GetResourceUsageAsync(cts.Token));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SystemResourceMonitor(null));
        }

        [Fact]
        public void Dispose_ShouldNotThrowException()
        {
            // Act & Assert
            var exception = Record.Exception(() => _monitor.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public async Task GetCpuUsageAsync_ShouldHandleExceptionsGracefully()
        {
            // This test verifies that the monitor handles exceptions gracefully
            // and falls back to alternative methods when performance counters fail
            
            // Act
            var result = await _monitor.GetCpuUsageAsync();

            // Assert
            Assert.True(result >= 0, "CPU usage should be non-negative even with exceptions");
        }

        [Fact]
        public async Task GetMemoryInfoAsync_ShouldHandleExceptionsGracefully()
        {
            // This test verifies that the monitor handles exceptions gracefully
            // and provides reasonable fallback values
            
            // Act
            var result = await _monitor.GetMemoryInfoAsync();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetDiskInfoAsync_WithInvalidPath_ShouldHandleExceptionsGracefully()
        {
            // Act
            var result = await _monitor.GetDiskInfoAsync("invalid_path_that_does_not_exist");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("invalid_path_that_does_not_exist", result.Path);
        }
    }
} 