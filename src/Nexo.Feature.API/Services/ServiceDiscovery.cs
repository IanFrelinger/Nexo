using Microsoft.Extensions.Logging;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using System.Collections.Concurrent;

namespace Nexo.Feature.API.Services;

/// <summary>
/// Service discovery implementation for dynamic service registration and discovery
/// </summary>
public class ServiceDiscovery : IServiceDiscovery
{
    private readonly ILogger<ServiceDiscovery> _logger;
    private readonly ConcurrentDictionary<string, ServiceInfo> _services = new();
    private readonly ConcurrentDictionary<string, ServiceHealthStatus> _healthStatuses = new();
    private readonly Timer _healthCheckTimer;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);

    public ServiceDiscovery(ILogger<ServiceDiscovery> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Start periodic health checks
        _healthCheckTimer = new Timer(PerformPeriodicHealthCheck, null, _healthCheckInterval, _healthCheckInterval);
    }

    /// <summary>
    /// Registers a new service with the discovery system
    /// </summary>
    public async Task<ServiceRegistrationResult> RegisterServiceAsync(ServiceInfo service)
    {
        try
        {
            if (string.IsNullOrEmpty(service.ServiceId))
            {
                return new ServiceRegistrationResult
                {
                    IsSuccess = false,
                    ServiceId = service.ServiceId,
                    ErrorMessage = "Service ID is required"
                };
            }

            if (_services.ContainsKey(service.ServiceId))
            {
                return new ServiceRegistrationResult
                {
                    IsSuccess = false,
                    ServiceId = service.ServiceId,
                    ErrorMessage = "Service with this ID is already registered"
                };
            }

            // Set default values
            service.RegisteredAt = DateTime.UtcNow;
            service.HealthStatus = Enums.ServiceHealthStatus.Unknown;

            // Add to services dictionary
            if (_services.TryAdd(service.ServiceId, service))
            {
                // Initialize health status
                _healthStatuses.TryAdd(service.ServiceId, new ServiceHealthStatus
                {
                    ServiceId = service.ServiceId,
                    Status = Enums.ServiceHealthStatus.Unknown,
                    CheckedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Successfully registered service {ServiceId} ({ServiceName})", 
                    service.ServiceId, service.Name);

                return new ServiceRegistrationResult
                {
                    IsSuccess = true,
                    ServiceId = service.ServiceId,
                    RegisteredAt = DateTime.UtcNow
                };
            }
            else
            {
                return new ServiceRegistrationResult
                {
                    IsSuccess = false,
                    ServiceId = service.ServiceId,
                    ErrorMessage = "Failed to register service"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering service {ServiceId}", service.ServiceId);
            
            return new ServiceRegistrationResult
            {
                IsSuccess = false,
                ServiceId = service.ServiceId,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Unregisters a service from the discovery system
    /// </summary>
    public async Task<ServiceUnregistrationResult> UnregisterServiceAsync(string serviceId)
    {
        try
        {
            if (string.IsNullOrEmpty(serviceId))
            {
                return new ServiceUnregistrationResult
                {
                    IsSuccess = false,
                    ServiceId = serviceId,
                    ErrorMessage = "Service ID is required"
                };
            }

            var serviceRemoved = _services.TryRemove(serviceId, out _);
            var healthStatusRemoved = _healthStatuses.TryRemove(serviceId, out _);

            if (serviceRemoved)
            {
                _logger.LogInformation("Successfully unregistered service {ServiceId}", serviceId);
                
                return new ServiceUnregistrationResult
                {
                    IsSuccess = true,
                    ServiceId = serviceId,
                    UnregisteredAt = DateTime.UtcNow
                };
            }
            else
            {
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

    /// <summary>
    /// Discovers available services based on criteria
    /// </summary>
    public async Task<IEnumerable<ServiceInfo>> DiscoverServicesAsync(ServiceDiscoveryCriteria criteria)
    {
        try
        {
            var services = _services.Values.AsEnumerable();

            // Filter by tags
            if (criteria.Tags.Any())
            {
                services = services.Where(s => criteria.Tags.Any(tag => s.Tags.Contains(tag)));
            }

            // Filter by name pattern
            if (!string.IsNullOrEmpty(criteria.NamePattern))
            {
                services = services.Where(s => s.Name.Contains(criteria.NamePattern, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by health status
            if (criteria.OnlyHealthy)
            {
                services = services.Where(s => s.HealthStatus == Enums.ServiceHealthStatus.Healthy);
            }
            else if (criteria.MinHealthStatus != Enums.ServiceHealthStatus.Unknown)
            {
                services = services.Where(s => s.HealthStatus >= criteria.MinHealthStatus);
            }

            // Apply max results limit
            if (criteria.MaxResults > 0)
            {
                services = services.Take(criteria.MaxResults);
            }

            return services.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering services");
            return Enumerable.Empty<ServiceInfo>();
        }
    }

    /// <summary>
    /// Gets a specific service by its identifier
    /// </summary>
    public async Task<ServiceInfo?> GetServiceAsync(string serviceId)
    {
        try
        {
            if (string.IsNullOrEmpty(serviceId))
            {
                return null;
            }

            _services.TryGetValue(serviceId, out var service);
            return service;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service {ServiceId}", serviceId);
            return null;
        }
    }

    /// <summary>
    /// Updates service health status
    /// </summary>
    public async Task<ServiceHealthUpdateResult> UpdateServiceHealthAsync(string serviceId, ServiceHealthStatus healthStatus)
    {
        try
        {
            if (string.IsNullOrEmpty(serviceId))
            {
                return new ServiceHealthUpdateResult
                {
                    IsSuccess = false,
                    ServiceId = serviceId,
                    ErrorMessage = "Service ID is required"
                };
            }

            var previousStatus = Enums.ServiceHealthStatus.Unknown;
            if (_healthStatuses.TryGetValue(serviceId, out var existingStatus))
            {
                previousStatus = existingStatus.Status;
            }

            // Update health status
            _healthStatuses.AddOrUpdate(serviceId, healthStatus, (key, oldValue) => healthStatus);

            // Update service info
            if (_services.TryGetValue(serviceId, out var service))
            {
                service.HealthStatus = healthStatus.Status;
                service.LastHealthCheck = healthStatus.CheckedAt;
            }

            _logger.LogDebug("Updated health status for service {ServiceId}: {PreviousStatus} -> {NewStatus}", 
                serviceId, previousStatus, healthStatus.Status);

            return new ServiceHealthUpdateResult
            {
                IsSuccess = true,
                ServiceId = serviceId,
                PreviousStatus = previousStatus,
                NewStatus = healthStatus.Status,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating health status for service {ServiceId}", serviceId);
            
            return new ServiceHealthUpdateResult
            {
                IsSuccess = false,
                ServiceId = serviceId,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets all registered services
    /// </summary>
    public async Task<IEnumerable<ServiceInfo>> GetAllServicesAsync()
    {
        try
        {
            return _services.Values.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all services");
            return Enumerable.Empty<ServiceInfo>();
        }
    }

    /// <summary>
    /// Performs health check on all registered services
    /// </summary>
    public async Task<IEnumerable<ServiceHealthStatus>> PerformHealthCheckAsync()
    {
        try
        {
            var healthChecks = new List<ServiceHealthStatus>();
            var tasks = new List<Task<ServiceHealthStatus>>();

            foreach (var service in _services.Values)
            {
                tasks.Add(CheckServiceHealthAsync(service));
            }

            var results = await Task.WhenAll(tasks);
            healthChecks.AddRange(results);

            return healthChecks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health checks");
            return Enumerable.Empty<ServiceHealthStatus>();
        }
    }

    /// <summary>
    /// Performs periodic health check
    /// </summary>
    private async void PerformPeriodicHealthCheck(object? state)
    {
        try
        {
            _logger.LogDebug("Starting periodic health check for {ServiceCount} services", _services.Count);
            
            var healthChecks = await PerformHealthCheckAsync();
            
            var healthyCount = healthChecks.Count(h => h.Status == Enums.ServiceHealthStatus.Healthy);
            var unhealthyCount = healthChecks.Count(h => h.Status == Enums.ServiceHealthStatus.Unhealthy);
            
            _logger.LogInformation("Health check completed: {HealthyCount} healthy, {UnhealthyCount} unhealthy", 
                healthyCount, unhealthyCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic health check");
        }
    }

    /// <summary>
    /// Checks health of a specific service
    /// </summary>
    private async Task<ServiceHealthStatus> CheckServiceHealthAsync(ServiceInfo service)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var healthStatus = new ServiceHealthStatus
        {
            ServiceId = service.ServiceId,
            Status = Enums.ServiceHealthStatus.Unknown,
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            // TODO: Implement actual health check logic
            // For now, simulate health check
            await Task.Delay(10); // Simulate network delay
            
            // Simulate health check result (random for demo)
            var random = new Random();
            var isHealthy = random.Next(100) < 80; // 80% chance of being healthy
            
            healthStatus.Status = isHealthy ? Enums.ServiceHealthStatus.Healthy : Enums.ServiceHealthStatus.Unhealthy;
            healthStatus.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            
            if (!isHealthy)
            {
                healthStatus.ErrorMessage = "Simulated health check failure";
            }
        }
        catch (Exception ex)
        {
            healthStatus.Status = Enums.ServiceHealthStatus.Unhealthy;
            healthStatus.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Health check failed for service {ServiceId}", service.ServiceId);
        }
        finally
        {
            stopwatch.Stop();
            healthStatus.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        }

        // Update the service's health status
        await UpdateServiceHealthAsync(service.ServiceId, healthStatus);
        
        return healthStatus;
    }

    /// <summary>
    /// Disposes the service discovery
    /// </summary>
    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
    }
} 