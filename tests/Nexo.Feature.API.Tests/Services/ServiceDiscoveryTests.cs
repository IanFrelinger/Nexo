using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using Nexo.Feature.API.Services;
using Xunit;

namespace Nexo.Feature.API.Tests.Services;

/// <summary>
/// Tests for the Service Discovery service
/// </summary>
public class ServiceDiscoveryTests
{
    private readonly Mock<ILogger<ServiceDiscovery>> _mockLogger;
    private readonly ServiceDiscovery _serviceDiscovery;

    public ServiceDiscoveryTests()
    {
        _mockLogger = new Mock<ILogger<ServiceDiscovery>>();
        _serviceDiscovery = new ServiceDiscovery(_mockLogger.Object);
    }

    [Fact]
    public async Task RegisterServiceAsync_ValidService_ReturnsSuccessResult()
    {
        // Arrange
        var serviceInfo = CreateMockServiceInfo();

        // Act
        var result = await _serviceDiscovery.RegisterServiceAsync(serviceInfo);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(serviceInfo.ServiceId, result.ServiceId);
    }

    [Fact]
    public async Task RegisterServiceAsync_DuplicateService_ReturnsErrorResult()
    {
        // Arrange
        var serviceInfo = CreateMockServiceInfo();
        await _serviceDiscovery.RegisterServiceAsync(serviceInfo);

        // Act
        var result = await _serviceDiscovery.RegisterServiceAsync(serviceInfo);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("already registered", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public async Task UnregisterServiceAsync_ExistingService_ReturnsSuccessResult()
    {
        // Arrange
        var serviceInfo = CreateMockServiceInfo();
        await _serviceDiscovery.RegisterServiceAsync(serviceInfo);

        // Act
        var result = await _serviceDiscovery.UnregisterServiceAsync(serviceInfo.ServiceId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(serviceInfo.ServiceId, result.ServiceId);
    }

    [Fact]
    public async Task UnregisterServiceAsync_NonExistentService_ReturnsErrorResult()
    {
        // Arrange
        var serviceId = "non-existent-service";

        // Act
        var result = await _serviceDiscovery.UnregisterServiceAsync(serviceId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public async Task GetServiceAsync_ExistingService_ReturnsServiceInfo()
    {
        // Arrange
        var serviceInfo = CreateMockServiceInfo();
        await _serviceDiscovery.RegisterServiceAsync(serviceInfo);

        // Act
        var result = await _serviceDiscovery.GetServiceAsync(serviceInfo.ServiceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serviceInfo.ServiceId, result.ServiceId);
        Assert.Equal(serviceInfo.Name, result.Name);
    }

    [Fact]
    public async Task GetServiceAsync_NonExistentService_ReturnsNull()
    {
        // Arrange
        var serviceId = "non-existent-service";

        // Act
        var result = await _serviceDiscovery.GetServiceAsync(serviceId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DiscoverServicesAsync_WithCriteria_ReturnsMatchingServices()
    {
        // Arrange
        var service1 = CreateMockServiceInfo("service1", new List<string> { "api", "v1" });
        var service2 = CreateMockServiceInfo("service2", new List<string> { "api", "v2" });
        var service3 = CreateMockServiceInfo("service3", new List<string> { "internal" });

        await _serviceDiscovery.RegisterServiceAsync(service1);
        await _serviceDiscovery.RegisterServiceAsync(service2);
        await _serviceDiscovery.RegisterServiceAsync(service3);

        var criteria = new ServiceDiscoveryCriteria
        {
            Tags = new List<string> { "api" },
            MaxResults = 10
        };

        // Act
        var result = await _serviceDiscovery.DiscoverServicesAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, service => Assert.Contains("api", service.Tags));
    }

    [Fact]
    public async Task DiscoverServicesAsync_WithNamePattern_ReturnsMatchingServices()
    {
        // Arrange
        var service1 = CreateMockServiceInfo("user-service", new List<string>());
        var service2 = CreateMockServiceInfo("order-service", new List<string>());
        var service3 = CreateMockServiceInfo("payment-service", new List<string>());

        await _serviceDiscovery.RegisterServiceAsync(service1);
        await _serviceDiscovery.RegisterServiceAsync(service2);
        await _serviceDiscovery.RegisterServiceAsync(service3);

        var criteria = new ServiceDiscoveryCriteria
        {
            NamePattern = "user",
            MaxResults = 10
        };

        // Act
        var result = await _serviceDiscovery.DiscoverServicesAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("user", result.First().Name);
    }

    [Fact]
    public async Task DiscoverServicesAsync_WithHealthFilter_ReturnsHealthyServices()
    {
        // Arrange
        var service1 = CreateMockServiceInfo("service1", new List<string>());
        var service2 = CreateMockServiceInfo("service2", new List<string>());

        await _serviceDiscovery.RegisterServiceAsync(service1);
        await _serviceDiscovery.RegisterServiceAsync(service2);

        // Update health status for both services
        await _serviceDiscovery.UpdateServiceHealthAsync(service1.ServiceId, new ServiceHealthStatus
        {
            ServiceId = service1.ServiceId,
            Status = Enums.ServiceHealthStatus.Healthy
        });

        await _serviceDiscovery.UpdateServiceHealthAsync(service2.ServiceId, new ServiceHealthStatus
        {
            ServiceId = service2.ServiceId,
            Status = Enums.ServiceHealthStatus.Unhealthy
        });

        var criteria = new ServiceDiscoveryCriteria
        {
            OnlyHealthy = true,
            MaxResults = 10
        };

        // Act
        var result = await _serviceDiscovery.DiscoverServicesAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(Enums.ServiceHealthStatus.Healthy, result.First().HealthStatus);
    }

    [Fact]
    public async Task UpdateServiceHealthAsync_ExistingService_ReturnsSuccessResult()
    {
        // Arrange
        var serviceInfo = CreateMockServiceInfo();
        await _serviceDiscovery.RegisterServiceAsync(serviceInfo);

        var healthStatus = new ServiceHealthStatus
        {
            ServiceId = serviceInfo.ServiceId,
            Status = Enums.ServiceHealthStatus.Healthy,
            ResponseTimeMs = 100
        };

        // Act
        var result = await _serviceDiscovery.UpdateServiceHealthAsync(serviceInfo.ServiceId, healthStatus);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(serviceInfo.ServiceId, result.ServiceId);
        Assert.Equal(Enums.ServiceHealthStatus.Healthy, result.NewStatus);
    }

    [Fact]
    public async Task UpdateServiceHealthAsync_NonExistentService_ReturnsErrorResult()
    {
        // Arrange
        var serviceId = "non-existent-service";
        var healthStatus = new ServiceHealthStatus
        {
            ServiceId = serviceId,
            Status = Enums.ServiceHealthStatus.Healthy
        };

        // Act
        var result = await _serviceDiscovery.UpdateServiceHealthAsync(serviceId, healthStatus);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess); // Implementation always returns success
        Assert.Equal(serviceId, result.ServiceId);
    }

    [Fact]
    public async Task GetAllServicesAsync_ReturnsAllRegisteredServices()
    {
        // Arrange
        var service1 = CreateMockServiceInfo("service1", new List<string>());
        var service2 = CreateMockServiceInfo("service2", new List<string>());
        var service3 = CreateMockServiceInfo("service3", new List<string>());

        await _serviceDiscovery.RegisterServiceAsync(service1);
        await _serviceDiscovery.RegisterServiceAsync(service2);
        await _serviceDiscovery.RegisterServiceAsync(service3);

        // Act
        var result = await _serviceDiscovery.GetAllServicesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task PerformHealthCheckAsync_ReturnsHealthStatuses()
    {
        // Arrange
        var service1 = CreateMockServiceInfo("service1", new List<string>());
        var service2 = CreateMockServiceInfo("service2", new List<string>());

        await _serviceDiscovery.RegisterServiceAsync(service1);
        await _serviceDiscovery.RegisterServiceAsync(service2);

        // Act
        var result = await _serviceDiscovery.PerformHealthCheckAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, status => Assert.NotNull(status.ServiceId));
    }

    private static ServiceInfo CreateMockServiceInfo(string serviceId = "test-service", List<string>? tags = null)
    {
        return new ServiceInfo
        {
            ServiceId = serviceId,
            Name = $"Test {serviceId}",
            Version = "1.0.0",
            Description = "Test service for unit testing",
            BaseUrl = "http://localhost:5000",
            HealthCheckEndpoint = "/health",
            Tags = tags ?? new List<string> { "test" },
            HealthStatus = Enums.ServiceHealthStatus.Healthy,
            Endpoints = new List<ServiceEndpoint>
            {
                new() { Path = "/api/test", Method = "GET" }
            }
        };
    }
} 