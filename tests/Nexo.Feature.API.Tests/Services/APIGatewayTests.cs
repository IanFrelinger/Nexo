using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using Nexo.Feature.API.Services;
using Xunit;

namespace Nexo.Feature.API.Tests.Services;

/// <summary>
/// Tests for the API Gateway service
/// </summary>
public class APIGatewayTests
{
    private readonly Mock<ILogger<APIGateway>> _mockLogger;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly APIGateway _apiGateway;

    public APIGatewayTests()
    {
        _mockLogger = new Mock<ILogger<APIGateway>>();
        _mockHttpClient = new Mock<HttpClient>();
        _apiGateway = new APIGateway(_mockLogger.Object, _mockHttpClient.Object);
    }

    [Fact]
    public async Task RouteRequestAsync_InvalidMethod_Returns400Response()
    {
        // Arrange
        var request = CreateMockAPIRequest("INVALID", "/api/test");

        // Act
        var result = await _apiGateway.RouteRequestAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("invalid http method", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public async Task RouteRequestAsync_ServiceNotFound_Returns404Response()
    {
        // Arrange
        var request = CreateMockAPIRequest("GET", "/api/nonexistent");

        // Act
        var result = await _apiGateway.RouteRequestAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("no service found", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public async Task ValidateRequestAsync_InvalidMethod_ReturnsInvalidResult()
    {
        // Arrange
        var request = CreateMockAPIRequest("INVALID", "/api/test");

        // Act
        var result = await _apiGateway.ValidateRequestAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid); // The implementation correctly validates HTTP methods
        Assert.Contains("Invalid HTTP method", result.Errors.First());
    }

    [Fact]
    public async Task RegisterServiceAsync_ValidService_ReturnsSuccessResult()
    {
        // Arrange
        var serviceRegistration = new ServiceRegistration
        {
            Service = CreateMockServiceInfo()
        };

        // Act
        var result = await _apiGateway.RegisterServiceAsync(serviceRegistration);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(serviceRegistration.Service.ServiceId, result.ServiceId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterServiceAsync_InvalidService_ReturnsFailureResult()
    {
        // Arrange
        var serviceRegistration = new ServiceRegistration
        {
            Service = new ServiceInfo
            {
                ServiceId = "test-service",
                Name = "", // Invalid: empty name
                BaseUrl = "http://localhost:8080"
            }
        };

        // Act
        var result = await _apiGateway.RegisterServiceAsync(serviceRegistration);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal(serviceRegistration.Service.ServiceId, result.ServiceId);
        Assert.Contains("invalid service registration", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public async Task UnregisterServiceAsync_ValidServiceId_ReturnsSuccessResult()
    {
        // Arrange
        var serviceRegistration = new ServiceRegistration
        {
            Service = CreateMockServiceInfo()
        };
        await _apiGateway.RegisterServiceAsync(serviceRegistration);

        // Act
        var result = await _apiGateway.UnregisterServiceAsync(serviceRegistration.Service.ServiceId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(serviceRegistration.Service.ServiceId, result.ServiceId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task GetHealthStatusAsync_ReturnsHealthStatus()
    {
        // Act
        var result = await _apiGateway.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Enums.HealthStatus.Healthy, result.Status);
        Assert.True(result.UptimeSeconds >= 0);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsMetrics()
    {
        // Act
        var result = await _apiGateway.GetMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalRequests >= 0);
        Assert.True(result.SuccessfulRequests >= 0);
        Assert.True(result.FailedRequests >= 0);
        Assert.True(result.AverageResponseTimeMs >= 0);
        Assert.NotNull(result.ServiceMetrics);
    }

    [Fact]
    public async Task RouteRequestAsync_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var serviceRegistration = new ServiceRegistration
        {
            Service = CreateMockServiceInfo()
        };
        await _apiGateway.RegisterServiceAsync(serviceRegistration);

        var request = CreateMockAPIRequest("GET", "/api/test");

        // Act
        var result = await _apiGateway.RouteRequestAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.RequestId, result.RequestId);
        Assert.True(result.ProcessingTimeMs >= 0);
    }

    [Fact]
    public async Task RouteRequestAsync_WithQueryParameters_ProcessesCorrectly()
    {
        // Arrange
        var serviceRegistration = new ServiceRegistration
        {
            Service = CreateMockServiceInfo()
        };
        await _apiGateway.RegisterServiceAsync(serviceRegistration);

        var request = CreateMockAPIRequest("GET", "/api/test");
        request.QueryParameters.Add("param1", "value1");
        request.QueryParameters.Add("param2", "value2");

        // Act
        var result = await _apiGateway.RouteRequestAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.RequestId, result.RequestId);
    }

    [Fact]
    public async Task RouteRequestAsync_WithHeaders_ProcessesCorrectly()
    {
        // Arrange
        var serviceRegistration = new ServiceRegistration
        {
            Service = CreateMockServiceInfo()
        };
        await _apiGateway.RegisterServiceAsync(serviceRegistration);

        var request = CreateMockAPIRequest("GET", "/api/test");
        request.Headers.Add("Authorization", "Bearer token");
        request.Headers.Add("Content-Type", "application/json");

        // Act
        var result = await _apiGateway.RouteRequestAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.RequestId, result.RequestId);
    }

    private static APIRequest CreateMockAPIRequest(string method, string path)
    {
        return new APIRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = method,
            Path = path,
            Headers = new Dictionary<string, string>(),
            QueryParameters = new Dictionary<string, string>(),
            Body = "",
            ContentType = "application/json"
        };
    }

    private static ServiceInfo CreateMockServiceInfo()
    {
        return new ServiceInfo
        {
            ServiceId = "test-service",
            Name = "Test Service",
            Version = "1.0.0",
            Description = "A test service",
            BaseUrl = "http://localhost:8080",
            HealthCheckEndpoint = "/health",
            Tags = new List<string> { "test", "api" },
            Metadata = new Dictionary<string, object>(),
            RegisteredAt = DateTime.UtcNow,
            HealthStatus = Enums.ServiceHealthStatus.Healthy,
            Endpoints = new List<ServiceEndpoint>
            {
                new ServiceEndpoint
                {
                    Path = "/api/test",
                    Method = "GET",
                    Description = "Test endpoint",
                    RequiresAuthentication = false,
                    RequiredPermissions = new List<string>(),
                    RateLimitConfig = null
                }
            },
            Configuration = new ServiceConfiguration(),
            IsEnabled = true
        };
    }
} 