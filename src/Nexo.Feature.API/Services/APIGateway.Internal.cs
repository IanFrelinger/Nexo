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
    /// Internal utility methods for service discovery and metrics
    /// </summary>
    public partial class APIGateway
    {
        private ServiceInfo? FindService(string path, string method)
        {
            lock (_lockObject)
            {
                // Match services based on endpoint paths, not base URL
                return _registeredServices.Values
                    .Where(s => s.IsEnabled && s.HealthStatus == Enums.ServiceHealthStatus.Healthy)
                    .FirstOrDefault(s => s.Endpoints.Any(e => e.Method.ToUpper() == method.ToUpper() && 
                                        path.StartsWith(e.Path, StringComparison.OrdinalIgnoreCase)));
            }
        }

        private async Task<APIResponse> ForwardRequestToServiceAsync(APIRequest request, ServiceInfo service)
        {
            try
            {
                // Find the matching endpoint to get the correct path
                var matchingEndpoint = service.Endpoints.FirstOrDefault(e => 
                    e.Method.ToUpper() == request.Method.ToUpper() && 
                    request.Path.StartsWith(e.Path, StringComparison.OrdinalIgnoreCase));
                
                if (matchingEndpoint == null)
                {
                    return new APIResponse
                    {
                        StatusCode = 404,
                        ErrorMessage = $"No matching endpoint found for {request.Method} {request.Path}",
                        RequestId = request.RequestId
                    };
                }

                // Construct target URL using service base URL and the endpoint path
                var targetUrl = $"{service.BaseUrl.TrimEnd('/')}{matchingEndpoint.Path}";
                
                using var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), targetUrl);

                // Add headers
                foreach (var header in request.Headers)
                {
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Add query parameters
                if (request.QueryParameters.Any())
                {
                    var queryString = string.Join("&", 
                        request.QueryParameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    targetUrl += $"?{queryString}";
                    httpRequest.RequestUri = new Uri(targetUrl);
                }

                // Add body for POST/PUT requests
                if (!string.IsNullOrEmpty(request.Body) && 
                    (request.Method.ToUpper() == "POST" || request.Method.ToUpper() == "PUT"))
                {
                    httpRequest.Content = new StringContent(request.Body, 
                        System.Text.Encoding.UTF8, request.ContentType);
                }

                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseBody = await httpResponse.Content.ReadAsStringAsync();

                return new APIResponse
                {
                    StatusCode = (int)httpResponse.StatusCode,
                    Body = responseBody,
                    ContentType = httpResponse.Content.Headers.ContentType?.ToString() ?? "application/json",
                    Headers = httpResponse.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                    RequestId = request.RequestId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding request to service {ServiceName}", service.Name);
                return new APIResponse
                {
                    StatusCode = 502,
                    ErrorMessage = $"Error forwarding request to service: {ex.Message}",
                    RequestId = request.RequestId
                };
            }
        }

        private void UpdateServiceMetrics(string serviceName, long responseTime, bool isSuccess)
        {
            lock (_lockObject)
            {
                if (_serviceMetrics.TryGetValue(serviceName, out var metrics))
                {
                    metrics.RequestCount++;
                    metrics.LastRequestTime = DateTime.UtcNow;

                    // Update average response time
                    var totalTime = metrics.AverageResponseTimeMs * (metrics.RequestCount - 1) + responseTime;
                    metrics.AverageResponseTimeMs = totalTime / metrics.RequestCount;

                    if (!isSuccess)
                    {
                        metrics.ErrorCount++;
                    }
                }
            }
        }

        private void UpdateGlobalMetrics(long responseTime, bool isSuccess)
        {
            lock (_lockObject)
            {
                _totalRequests++;
                if (isSuccess)
                {
                    _successfulRequests++;
                }
                else
                {
                    _failedRequests++;
                }

                _responseTimes.Add(responseTime);
                
                // Keep only last 1000 response times to prevent memory issues
                if (_responseTimes.Count > 1000)
                {
                    _responseTimes.RemoveAt(0);
                }
            }
        }
    }
}
