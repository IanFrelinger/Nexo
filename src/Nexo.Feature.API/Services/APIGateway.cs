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
    /// Implementation of the API Gateway for centralized API management and routing.
    /// </summary>
    public class APIGateway : IAPIGateway
    {
        private readonly ILogger<APIGateway> _logger;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, ServiceInfo> _registeredServices;
        private readonly Dictionary<string, ServiceMetrics> _serviceMetrics;
        private readonly object _lockObject = new object();
        private readonly DateTime _startTime = DateTime.UtcNow;
        private long _totalRequests = 0;
        private long _successfulRequests = 0;
        private long _failedRequests = 0;
        private readonly List<long> _responseTimes = new List<long>();

        public APIGateway(ILogger<APIGateway> logger, HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _registeredServices = new Dictionary<string, ServiceInfo>();
            _serviceMetrics = new Dictionary<string, ServiceMetrics>();
        }

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

        public async Task<ServiceRegistrationResult> RegisterServiceAsync(ServiceRegistration serviceRegistration)
        {
            try
            {
                if (string.IsNullOrEmpty(serviceRegistration.Service.Name) || string.IsNullOrEmpty(serviceRegistration.Service.BaseUrl))
                {
                    _logger.LogWarning("Invalid service registration: Name or BaseUrl is null or empty");
                    return new ServiceRegistrationResult
                    {
                        IsSuccess = false,
                        ServiceId = serviceRegistration.Service.ServiceId,
                        ErrorMessage = "Invalid service registration: Name or BaseUrl is null or empty"
                    };
                }

                lock (_lockObject)
                {
                    _registeredServices[serviceRegistration.Service.ServiceId] = serviceRegistration.Service;
                    _serviceMetrics[serviceRegistration.Service.Name] = new ServiceMetrics
                    {
                        ServiceName = serviceRegistration.Service.Name,
                        RequestCount = 0,
                        AverageResponseTimeMs = 0,
                        ErrorCount = 0,
                        LastRequestTime = DateTime.UtcNow
                    };
                }

                _logger.LogInformation("Registered service: {ServiceName} at {BaseUrl}", 
                    serviceRegistration.Service.Name, serviceRegistration.Service.BaseUrl);

                return new ServiceRegistrationResult
                {
                    IsSuccess = true,
                    ServiceId = serviceRegistration.Service.ServiceId,
                    RegisteredAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering service {ServiceName}", serviceRegistration.Service.Name);
                return new ServiceRegistrationResult
                {
                    IsSuccess = false,
                    ServiceId = serviceRegistration.Service.ServiceId,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ServiceUnregistrationResult> UnregisterServiceAsync(string serviceId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_registeredServices.TryGetValue(serviceId, out var service))
                    {
                        _registeredServices.Remove(serviceId);
                        _serviceMetrics.Remove(service.Name);
                        _logger.LogInformation("Unregistered service: {ServiceName}", service.Name);
                        return new ServiceUnregistrationResult
                        {
                            IsSuccess = true,
                            ServiceId = serviceId,
                            UnregisteredAt = DateTime.UtcNow
                        };
                    }

                    _logger.LogWarning("Service not found: {ServiceId}", serviceId);
                    return new ServiceUnregistrationResult
                    {
                        IsSuccess = false,
                        ServiceId = serviceId,
                        ErrorMessage = "Service not found"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering service {ServiceId}", serviceId);
                return new ServiceUnregistrationResult
                {
                    IsSuccess = false,
                    ServiceId = serviceId,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<IEnumerable<ServiceInfo>> GetRegisteredServicesAsync()
        {
            lock (_lockObject)
            {
                return _registeredServices.Values.ToList();
            }
        }

        public async Task<RequestValidationResult> ValidateRequestAsync(APIRequest request)
        {
            var result = new RequestValidationResult { IsValid = true };

            // Validate required fields
            if (string.IsNullOrEmpty(request.Method))
            {
                result.Errors.Add("HTTP method is required");
                result.IsValid = false;
            }

            if (string.IsNullOrEmpty(request.Path))
            {
                result.Errors.Add("Request path is required");
                result.IsValid = false;
            }

            // Validate HTTP method
            var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
            if (!validMethods.Contains(request.Method.ToUpper()))
            {
                result.Errors.Add($"Invalid HTTP method: {request.Method}");
                result.IsValid = false;
            }

            // Validate content type for POST/PUT requests
            if ((request.Method.ToUpper() == "POST" || request.Method.ToUpper() == "PUT") && 
                string.IsNullOrEmpty(request.ContentType))
            {
                result.Warnings.Add("Content-Type header is recommended for POST/PUT requests");
            }

            return result;
        }

        public async Task<APIRequest> TransformRequestAsync(APIRequest request)
        {
            // Add correlation headers
            request.Headers["X-Request-ID"] = request.RequestId;
            request.Headers["X-Gateway-Timestamp"] = DateTime.UtcNow.ToString("O");

            // Add user agent if not present
            if (!request.Headers.ContainsKey("User-Agent"))
            {
                request.Headers["User-Agent"] = "Nexo-API-Gateway/1.0";
            }

            return request;
        }

        public async Task<APIResponse> TransformResponseAsync(APIResponse response)
        {
            // Add gateway headers
            response.Headers["X-Gateway-Processing-Time"] = response.ProcessingTimeMs.ToString();
            response.Headers["X-Gateway-Timestamp"] = DateTime.UtcNow.ToString("O");

            return response;
        }

        public async Task<GatewayHealthStatus> GetHealthStatusAsync()
        {
            var process = Process.GetCurrentProcess();
            var uptime = (DateTime.UtcNow - _startTime).TotalSeconds;

            return new GatewayHealthStatus
            {
                Status = HealthStatus.Healthy, // Simplified for now
                Timestamp = DateTime.UtcNow,
                UptimeSeconds = (long)uptime,
                MemoryUsageMB = (long)(process.WorkingSet64 / 1024.0 / 1024.0),
                CpuUsagePercentage = 0, // Would need more complex implementation
                ActiveConnections = _registeredServices.Count,
                Details = new Dictionary<string, object>
                {
                    ["RegisteredServices"] = _registeredServices.Count,
                    ["TotalRequests"] = _totalRequests,
                    ["SuccessRate"] = _totalRequests > 0 ? (double)_successfulRequests / _totalRequests * 100 : 0
                }
            };
        }

        public async Task<GatewayMetrics> GetMetricsAsync()
        {
            lock (_lockObject)
            {
                var averageResponseTime = _responseTimes.Count > 0 ? _responseTimes.Average() : 0;
                var requestsPerSecond = _totalRequests > 0 ? 
                    (double)_totalRequests / (DateTime.UtcNow - _startTime).TotalSeconds : 0;
                var errorRate = _totalRequests > 0 ? (double)_failedRequests / _totalRequests * 100 : 0;

                return new GatewayMetrics
                {
                    TotalRequests = _totalRequests,
                    SuccessfulRequests = _successfulRequests,
                    FailedRequests = _failedRequests,
                    AverageResponseTimeMs = averageResponseTime,
                    RequestsPerSecond = requestsPerSecond,
                    ErrorRatePercentage = errorRate,
                    Timestamp = DateTime.UtcNow,
                    ServiceMetrics = new Dictionary<string, ServiceMetrics>(_serviceMetrics)
                };
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