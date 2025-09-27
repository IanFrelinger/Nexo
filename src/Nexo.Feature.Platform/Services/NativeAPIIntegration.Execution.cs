using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Services
{
    /// <summary>
    /// API execution functionality
    /// </summary>
    public partial class NativeAPIIntegration : INativeAPIIntegration
    {
        public async Task<NativeAPICallResult> ExecuteAPICallAsync(string apiName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Executing native API call: {APIName}", apiName);

            try
            {
                if (!_isInitialized)
                {
                    throw new InvalidOperationException("Native API integration is not initialized");
                }

                // Check if API is available
                var availability = await CheckAPIAvailabilityAsync(apiName, cancellationToken);
                if (!availability.IsAvailable)
                {
                    return new NativeAPICallResult
                    {
                        IsSuccess = false,
                        Message = $"API {apiName} is not available: {availability.Reason}",
                        APIName = apiName,
                        Parameters = parameters,
                        Errors = new List<string> { availability.Reason }
                    };
                }

                // Check permissions if required
                if (_availableAPIs.TryGetValue(apiName, out var apiInfo) && apiInfo.RequiresPermission)
                {
                    var permissionStatus = await CheckPermissionStatusAsync(apiName, cancellationToken);
                    if (permissionStatus.Status != PermissionStatus.Granted)
                    {
                        return new NativeAPICallResult
                        {
                            IsSuccess = false,
                            Message = $"Permission denied for API {apiName}",
                            APIName = apiName,
                            Parameters = parameters,
                            Errors = new List<string> { "Permission denied" }
                        };
                    }
                }

                // Execute the API call
                object? result = null;
                if (_customHandlers.TryGetValue(apiName, out var handler))
                {
                    var handlerResult = await handler.HandleAPICallAsync(parameters, cancellationToken);
                    result = handlerResult.Result;
                }
                else
                {
                    result = await ExecutePlatformAPICallAsync(apiName, parameters, cancellationToken);
                }

                stopwatch.Stop();
                return new NativeAPICallResult
                {
                    IsSuccess = true,
                    Message = $"Successfully executed API call: {apiName}",
                    APIName = apiName,
                    Result = result,
                    Parameters = parameters,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error executing native API call: {APIName}", apiName);
                return new NativeAPICallResult
                {
                    IsSuccess = false,
                    Message = $"Error executing API call: {ex.Message}",
                    APIName = apiName,
                    Parameters = parameters,
                    Errors = new List<string> { ex.Message },
                    ExecutionTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<NativeAPIAvailabilityResult> CheckAPIAvailabilityAsync(string apiName, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            _logger.LogDebug("Checking API availability: {APIName}", apiName);

            try
            {
                if (_availableAPIs.TryGetValue(apiName, out var apiInfo))
                {
                    return new NativeAPIAvailabilityResult
                    {
                        IsAvailable = true,
                        APIName = apiName,
                        PlatformType = _currentPlatform,
                        Version = apiInfo.Version,
                        Reason = "API is available and supported"
                    };
                }

                return new NativeAPIAvailabilityResult
                {
                    IsAvailable = false,
                    APIName = apiName,
                    PlatformType = _currentPlatform,
                    Reason = "API is not available on this platform"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking API availability: {APIName}", apiName);
                return new NativeAPIAvailabilityResult
                {
                    IsAvailable = false,
                    APIName = apiName,
                    PlatformType = _currentPlatform,
                    Reason = $"Error checking availability: {ex.Message}"
                };
            }
        }

        public async Task<AvailableAPIsResult> GetAvailableAPIsAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            _logger.LogDebug("Getting available APIs for platform: {PlatformType}", _currentPlatform);

            try
            {
                return new AvailableAPIsResult
                {
                    IsSuccess = true,
                    Message = $"Retrieved {_availableAPIs.Count} available APIs",
                    PlatformType = _currentPlatform,
                    AvailableAPIs = _availableAPIs.Values.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available APIs");
                return new AvailableAPIsResult
                {
                    IsSuccess = false,
                    Message = $"Error getting available APIs: {ex.Message}",
                    PlatformType = _currentPlatform
                };
            }
        }
    }
}
