using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using Nexo.Feature.API.Services;
using Nexo.Feature.API.Enums;
using System.Net;
using System.Threading;

namespace Nexo.Feature.API.Tests
{
    /// <summary>
    /// Integration tests for API Gateway functionality
    /// </summary>
    public partial class APIGatewayTests
    {
        #region Integration Tests

        [Fact]
        public async Task APIGateway_CompleteWorkflow_WorksCorrectly()
        {
            // Arrange
            var service = new ServiceInfo
            {
                ServiceId = "integration-service",
                Name = "IntegrationService",
                BaseUrl = "https://api.integration.com",
                Endpoints = new List<ServiceEndpoint> 
                { 
                    new ServiceEndpoint { Path = "/api/data", Method = "GET" },
                    new ServiceEndpoint { Path = "/api/data", Method = "POST" }
                },
                HealthStatus = Enums.ServiceHealthStatus.Healthy,
                IsEnabled = true
            };
            var registration = new ServiceRegistration { Service = service };

            await _apiGateway.RegisterServiceAsync(registration);

            var request = new APIRequest
            {
                Method = "POST",
                Path = "/api/data",
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = "{\"test\":\"data\"}"
            };

            SetupMockHttpResponse(HttpStatusCode.Created, "{\"id\":123,\"status\":\"created\"}");

            // Act
            var response = await _apiGateway.RouteRequestAsync(request);

            // Assert
            Assert.Equal(201, response.StatusCode);
            Assert.Equal("{\"id\":123,\"status\":\"created\"}", response.Body);
            Assert.True(response.ProcessingTimeMs >= 0); // Processing time can be 0 for very fast operations

            // Verify metrics were updated
            var metrics = await _apiGateway.GetMetricsAsync();
            Assert.Equal(1, metrics.TotalRequests);
            Assert.Equal(1, metrics.SuccessfulRequests);
            Assert.Equal(0, metrics.FailedRequests);
        }

        [Theory]
        [InlineData("GET", "/api/users", 200)]
        [InlineData("POST", "/api/users", 201)]
        [InlineData("PUT", "/api/users/1", 200)]
        [InlineData("DELETE", "/api/users/1", 204)]
        public async Task APIGateway_DifferentHttpMethods_WorkCorrectly(string method, string path, int expectedStatusCode)
        {
            // Arrange
            var service = new ServiceInfo
            {
                ServiceId = "user-service-methods",
                Name = "UserService",
                BaseUrl = "https://api.users.com",
                Endpoints = new List<ServiceEndpoint> 
                { 
                    new ServiceEndpoint { Path = "/api/users", Method = "GET" },
                    new ServiceEndpoint { Path = "/api/users", Method = "POST" },
                    new ServiceEndpoint { Path = "/api/users/1", Method = "PUT" },
                    new ServiceEndpoint { Path = "/api/users/1", Method = "DELETE" }
                },
                HealthStatus = Enums.ServiceHealthStatus.Healthy,
                IsEnabled = true
            };
            var registration = new ServiceRegistration { Service = service };

            await _apiGateway.RegisterServiceAsync(registration);

            var request = new APIRequest
            {
                Method = method,
                Path = path
            };

            SetupMockHttpResponse((HttpStatusCode)expectedStatusCode, "{\"result\":\"success\"}");

            // Act
            var response = await _apiGateway.RouteRequestAsync(request);

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        #endregion
    }
}
