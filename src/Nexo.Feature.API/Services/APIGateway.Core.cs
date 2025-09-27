using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using Nexo.Feature.API.Enums;

namespace Nexo.Feature.API.Services
{
    /// <summary>
    /// Core API Gateway routing functionality
    /// </summary>
    public partial class APIGateway
    {
        public async Task<APIResponse> RouteRequestAsync(APIRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new APIResponse { RequestId = request.RequestId };

            try
            {
                _logger.LogInformation("Routing request {RequestId} to {Path}", request.RequestId, request.Path);

                // Validate request
                var validationResult = await ValidateRequestAsync(request);
                if (!validationResult.IsValid)
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = string.Join("; ", validationResult.Errors);
                    _logger.LogWarning("Request {RequestId} validation failed: {Errors}", 
                        request.RequestId, response.ErrorMessage);
                    return response;
                }

                // Transform request
                var transformedRequest = await TransformRequestAsync(request);

                // Find appropriate service
                var service = FindService(transformedRequest.Path, transformedRequest.Method);
                if (service == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = $"No service found for path: {transformedRequest.Path}";
                    _logger.LogWarning("No service found for request {RequestId} to {Path}", 
                        request.RequestId, transformedRequest.Path);
                    return response;
                }

                // Forward request to service
                var serviceResponse = await ForwardRequestToServiceAsync(transformedRequest, service);
                
                // Update metrics
                UpdateServiceMetrics(service.Name, stopwatch.ElapsedMilliseconds, serviceResponse.StatusCode < 400);

                // Transform response
                response = await TransformResponseAsync(serviceResponse);
                response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Request {RequestId} completed successfully in {ProcessingTime}ms", 
                    request.RequestId, response.ProcessingTimeMs);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error routing request {RequestId}", request.RequestId);
                response.StatusCode = 500;
                response.ErrorMessage = "Internal server error";
                response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                return response;
            }
            finally
            {
                stopwatch.Stop();
                UpdateGlobalMetrics(stopwatch.ElapsedMilliseconds, response.StatusCode < 400);
            }
        }

        /// <summary>
        /// Resets the API Gateway state for testing purposes
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                _registeredServices.Clear();
                _serviceMetrics.Clear();
                _totalRequests = 0;
                _successfulRequests = 0;
                _failedRequests = 0;
                _responseTimes.Clear();
            }
        }
    }
}
