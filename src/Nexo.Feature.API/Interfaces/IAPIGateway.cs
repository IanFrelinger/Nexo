using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.API.Models;

namespace Nexo.Feature.API.Interfaces
{
    /// <summary>
    /// Interface for centralized API management and routing.
    /// </summary>
    public interface IAPIGateway
    {
        /// <summary>
        /// Routes an incoming request to the appropriate service.
        /// </summary>
        /// <param name="request">The incoming API request.</param>
        /// <returns>The API response.</returns>
        Task<APIResponse> RouteRequestAsync(APIRequest request);

        /// <summary>
        /// Registers a new service.
        /// </summary>
        /// <param name="serviceRegistration">The service registration information.</param>
        /// <returns>Registration result.</returns>
        Task<ServiceRegistrationResult> RegisterServiceAsync(ServiceRegistration serviceRegistration);

        /// <summary>
        /// Unregisters a service.
        /// </summary>
        /// <param name="serviceId">The ID of the service to unregister.</param>
        /// <returns>Unregistration result.</returns>
        Task<ServiceUnregistrationResult> UnregisterServiceAsync(string serviceId);

        /// <summary>
        /// Gets all registered services.
        /// </summary>
        /// <returns>List of registered services.</returns>
        Task<IEnumerable<ServiceInfo>> GetRegisteredServicesAsync();

        /// <summary>
        /// Validates an incoming request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <returns>Validation result.</returns>
        Task<RequestValidationResult> ValidateRequestAsync(APIRequest request);

        /// <summary>
        /// Transforms a request before routing.
        /// </summary>
        /// <param name="request">The original request.</param>
        /// <returns>The transformed request.</returns>
        Task<APIRequest> TransformRequestAsync(APIRequest request);

        /// <summary>
        /// Transforms a response before returning to client.
        /// </summary>
        /// <param name="response">The original response.</param>
        /// <returns>The transformed response.</returns>
        Task<APIResponse> TransformResponseAsync(APIResponse response);

        /// <summary>
        /// Gets the health status of the API Gateway.
        /// </summary>
        /// <returns>Health status information.</returns>
        Task<GatewayHealthStatus> GetHealthStatusAsync();

        /// <summary>
        /// Gets metrics and statistics for the API Gateway.
        /// </summary>
        /// <returns>Gateway metrics.</returns>
        Task<GatewayMetrics> GetMetricsAsync();

        /// <summary>
        /// Resets the API Gateway state for testing purposes.
        /// </summary>
        void Reset();
    }
} 