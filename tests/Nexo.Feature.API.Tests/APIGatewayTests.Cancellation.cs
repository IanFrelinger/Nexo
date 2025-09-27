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
    /// Cancellation tests for API Gateway functionality.
    /// </summary>
    public partial class APIGatewayTests
    {
        #region Cancellation Tests

        [Fact]
        public async Task RouteRequestAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var request = new APIRequest
            {
                Method = "GET",
                Path = "/api/users"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.RouteRequestAsync(request, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task RegisterServiceAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var service = new ServiceInfo
            {
                ServiceId = "test-service",
                Name = "TestService",
                BaseUrl = "https://api.test.com"
            };
            var registration = new ServiceRegistration { Service = service };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.RegisterServiceAsync(registration, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task UnregisterServiceAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var serviceId = "test-service";

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.UnregisterServiceAsync(serviceId, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ValidateRequestAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var request = new APIRequest
            {
                Method = "GET",
                Path = "/api/users"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.ValidateRequestAsync(request, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task TransformRequestAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var request = new APIRequest
            {
                Method = "GET",
                Path = "/api/users"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.TransformRequestAsync(request, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task TransformResponseAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var response = new APIResponse
            {
                StatusCode = 200,
                ProcessingTimeMs = 150
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.TransformResponseAsync(response, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GetHealthStatusAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.GetHealthStatusAsync(cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GetMetricsAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.GetMetricsAsync(cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GetRegisteredServicesAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.GetRegisteredServicesAsync(cancellationTokenSource.Token));
        }

        [Fact]
        public async Task RouteRequestAsync_WithCancellationDuringExecution_HandlesGracefully()
        {
            // Arrange
            var request = new APIRequest
            {
                Method = "GET",
                Path = "/api/users"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            
            // Cancel after a short delay to simulate cancellation during execution
            _ = Task.Run(async () =>
            {
                await Task.Delay(10);
                cancellationTokenSource.Cancel();
            });

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _apiGateway.RouteRequestAsync(request, cancellationTokenSource.Token));
        }

        #endregion
    }
}
