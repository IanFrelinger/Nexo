using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Interface for managing configuration
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Get configuration value
    /// </summary>
    Task<T?> GetConfigurationAsync<T>(string key);
    
    /// <summary>
    /// Set configuration value
    /// </summary>
    Task SetConfigurationAsync<T>(string key, T value);
    
    /// <summary>
    /// Get all configuration
    /// </summary>
    Task<Dictionary<string, object>> GetAllConfigurationAsync();
    
    /// <summary>
    /// Reset configuration to defaults
    /// </summary>
    Task ResetToDefaultsAsync();
    
    /// <summary>
    /// Save configuration
    /// </summary>
    Task SaveConfigurationAsync();
    
    /// <summary>
    /// Load configuration
    /// </summary>
    Task LoadConfigurationAsync();
    
    /// <summary>
    /// Check if configuration exists
    /// </summary>
    Task<bool> HasConfigurationAsync(string key);
    
    /// <summary>
    /// Remove configuration
    /// </summary>
    Task RemoveConfigurationAsync(string key);
}
