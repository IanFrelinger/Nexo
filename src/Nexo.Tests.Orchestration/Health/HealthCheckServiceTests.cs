using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Health;
using Xunit;

namespace Nexo.Tests.Orchestration.Health;

/// <summary>Tests for health check service.</summary>
public class HealthCheckServiceTests
{
    private readonly Mock<ILogger<HealthCheckService>> _loggerMock;
    private readonly HealthCheckService _healthService;

    public HealthCheckServiceTests()
    {
        _loggerMock = new Mock<ILogger<HealthCheckService>>();
        _healthService = new HealthCheckService(logger: _loggerMock.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnknown_ForUnregisteredCheck()
    {
        // Act
        var status = await _healthService.CheckHealthAsync("unknown");

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.Status.Should().Be("Unknown");
        status.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_ForHealthyCheck()
    {
        // Arrange
        var mockCheck = new Mock<IHealthCheck>();
        mockCheck.Setup(x => x.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthStatus
            {
                Name = "test",
                IsHealthy = true,
                Status = "Healthy",
                Timestamp = DateTimeOffset.UtcNow
            });

        _healthService.Register("test", mockCheck.Object);

        // Act
        var status = await _healthService.CheckHealthAsync("test");

        // Assert
        status.IsHealthy.Should().BeTrue();
        status.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCacheResults()
    {
        // Arrange
        var callCount = 0;
        var mockCheck = new Mock<IHealthCheck>();
        mockCheck.Setup(x => x.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HealthStatus
                {
                    Name = "test",
                    IsHealthy = true,
                    Status = "Healthy",
                    Timestamp = DateTimeOffset.UtcNow
                };
            });

        _healthService.Register("test", mockCheck.Object);

        // Act
        await _healthService.CheckHealthAsync("test");
        await _healthService.CheckHealthAsync("test");
        await _healthService.CheckHealthAsync("test");

        // Assert
        callCount.Should().Be(1); // Should be cached
    }

    [Fact]
    public async Task CheckAllAsync_ShouldReturnAllStatuses()
    {
        // Arrange
        var mockCheck1 = new Mock<IHealthCheck>();
        mockCheck1.Setup(x => x.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthStatus
            {
                Name = "check1",
                IsHealthy = true,
                Status = "Healthy",
                Timestamp = DateTimeOffset.UtcNow
            });

        var mockCheck2 = new Mock<IHealthCheck>();
        mockCheck2.Setup(x => x.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthStatus
            {
                Name = "check2",
                IsHealthy = false,
                Status = "Unhealthy",
                Timestamp = DateTimeOffset.UtcNow
            });

        _healthService.Register("check1", mockCheck1.Object);
        _healthService.Register("check2", mockCheck2.Object);

        // Act
        var report = await _healthService.CheckAllAsync();

        // Assert
        report.Statuses.Should().HaveCount(2);
        report.OverallHealthy.Should().BeFalse(); // One is unhealthy
        report.Statuses.Should().Contain(s => s.Name == "check1" && s.IsHealthy);
        report.Statuses.Should().Contain(s => s.Name == "check2" && !s.IsHealthy);
    }

    [Fact]
    public async Task ClearCache_ShouldClearStatusCache()
    {
        // Arrange
        var callCount = 0;
        var mockCheck = new Mock<IHealthCheck>();
        mockCheck.Setup(x => x.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HealthStatus
                {
                    Name = "test",
                    IsHealthy = true,
                    Status = "Healthy",
                    Timestamp = DateTimeOffset.UtcNow
                };
            });

        _healthService.Register("test", mockCheck.Object);

        // Act
        await _healthService.CheckHealthAsync("test");
        _healthService.ClearCache();
        await _healthService.CheckHealthAsync("test");

        // Assert
        callCount.Should().Be(2); // Should call again after cache clear
    }
}

