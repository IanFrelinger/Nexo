using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services
{
    /// <summary>
    /// API handler management functionality
    /// </summary>
    public partial class NativeAPIIntegration : INativeAPIIntegration
    {
        public async Task<APIHandlerRegistrationResult> RegisterAPIHandlerAsync(string apiName, INativeAPIHandler handler, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            _logger.LogInformation("Registering custom API handler for: {APIName}", apiName);

            try
            {
                _customHandlers[apiName] = handler;
                return new APIHandlerRegistrationResult
                {
                    IsSuccess = true,
                    Message = $"Successfully registered custom handler for {apiName}",
                    APIName = apiName,
                    IsRegistered = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering API handler for: {APIName}", apiName);
                return new APIHandlerRegistrationResult
                {
                    IsSuccess = false,
                    Message = $"Error registering handler: {ex.Message}",
                    APIName = apiName,
                    IsRegistered = false
                };
            }
        }

        public async Task<APIHandlerRegistrationResult> UnregisterAPIHandlerAsync(string apiName, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            _logger.LogInformation("Unregistering custom API handler for: {APIName}", apiName);

            try
            {
                var wasRegistered = _customHandlers.Remove(apiName);
                return new APIHandlerRegistrationResult
                {
                    IsSuccess = true,
                    Message = wasRegistered ? $"Successfully unregistered handler for {apiName}" : $"No handler was registered for {apiName}",
                    APIName = apiName,
                    IsRegistered = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering API handler for: {APIName}", apiName);
                return new APIHandlerRegistrationResult
                {
                    IsSuccess = false,
                    Message = $"Error unregistering handler: {ex.Message}",
                    APIName = apiName,
                    IsRegistered = false
                };
            }
        }
    }
}
