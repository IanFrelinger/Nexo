using Nexo.Core.Domain.Entities.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Application.Services.Adaptation;

public class ConfigurationManager : IConfigurationManager
{
    private readonly Dictionary<string, object> _configurations = new();
    
    public Task ApplyConfigurationAsync(EnvironmentConfiguration configuration)
    {
        return Task.CompletedTask;
    }
    
    public Task<object> GetConfigurationAsync(string configurationType)
    {
        return Task.FromResult<object>(new object());
    }
    
    public Task SetConfigurationAsync(string configurationType, object value)
    {
        return Task.CompletedTask;
    }
    
    public Task<T?> GetConfigurationAsync<T>(string key)
    {
        if (_configurations.TryGetValue(key, out var value) && value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }
        return Task.FromResult<T?>(default);
    }
    
    public Task SetConfigurationAsync<T>(string key, T value)
    {
        _configurations[key] = value!;
        return Task.CompletedTask;
    }
    
    public Task<Dictionary<string, object>> GetAllConfigurationAsync()
    {
        return Task.FromResult(new Dictionary<string, object>(_configurations));
    }
    
    public Task ResetToDefaultsAsync()
    {
        _configurations.Clear();
        return Task.CompletedTask;
    }
    
    public Task SaveConfigurationAsync()
    {
        return Task.CompletedTask;
    }
    
    public Task LoadConfigurationAsync()
    {
        return Task.CompletedTask;
    }
    
    public Task<bool> HasConfigurationAsync(string key)
    {
        return Task.FromResult(_configurations.ContainsKey(key));
    }
    
    public Task RemoveConfigurationAsync(string key)
    {
        _configurations.Remove(key);
        return Task.CompletedTask;
    }
}
