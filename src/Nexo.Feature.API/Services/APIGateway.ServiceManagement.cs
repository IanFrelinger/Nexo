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
    /// Service registration and management functionality
    /// </summary>
    public partial class APIGateway
    {
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
    }
}
