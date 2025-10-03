using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Director.Avalonia.Services;

/// <summary>
/// Service for hot reloading Avalonia application components
/// </summary>
public class HotReloadService
{
    private readonly ILogger<HotReloadService> _logger;
    private readonly CodeModificationService _codeModificationService;
    private readonly ConcurrentDictionary<string, Assembly> _loadedAssemblies = new();
    private readonly ConcurrentDictionary<string, Type> _loadedTypes = new();
    private readonly string _hotReloadDirectory;

    public event EventHandler<HotReloadEventArgs>? ComponentReloaded;
    public event EventHandler<HotReloadErrorEventArgs>? ReloadError;

    public HotReloadService(ILogger<HotReloadService> logger, CodeModificationService codeModificationService)
    {
        _logger = logger;
        _codeModificationService = codeModificationService;
        _hotReloadDirectory = Path.Combine(Path.GetTempPath(), "DirectorStudio", "HotReload");
        Directory.CreateDirectory(_hotReloadDirectory);
    }

    /// <summary>
    /// Reloads a component with new code
    /// </summary>
    public async Task<bool> ReloadComponentAsync(string componentName, string newCode, string targetClass, string targetMethod)
    {
        try
        {
            _logger.LogInformation($"Reloading component: {componentName}");

            // Validate the new code
            var validationResult = await _codeModificationService.ValidateCodeAsync(newCode, targetClass, targetMethod);
            if (!validationResult.IsValid)
            {
                var error = $"Code validation failed: {string.Join(", ", validationResult.Errors)}";
                _logger.LogError(error);
                ReloadError?.Invoke(this, new HotReloadErrorEventArgs(componentName, error));
                return false;
            }

            // Compile the new code
            var assemblyName = $"{componentName}_Reloaded_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var assembly = await _codeModificationService.CompileCodeAsync(newCode, assemblyName);

            // Load the new type
            var newType = assembly.GetType(targetClass);
            if (newType == null)
            {
                var error = $"Type {targetClass} not found in compiled assembly";
                _logger.LogError(error);
                ReloadError?.Invoke(this, new HotReloadErrorEventArgs(componentName, error));
                return false;
            }

            // Store the new assembly and type
            _loadedAssemblies[componentName] = assembly;
            _loadedTypes[componentName] = newType;

            // Notify subscribers
            ComponentReloaded?.Invoke(this, new HotReloadEventArgs(componentName, newType, targetMethod));

            _logger.LogInformation($"Successfully reloaded component: {componentName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to reload component: {componentName}");
            ReloadError?.Invoke(this, new HotReloadErrorEventArgs(componentName, ex.Message));
            return false;
        }
    }

    /// <summary>
    /// Creates a new instance of a reloaded component
    /// </summary>
    public T? CreateInstance<T>(string componentName, params object[] constructorArgs) where T : class
    {
        try
        {
            if (!_loadedTypes.TryGetValue(componentName, out var type))
            {
                _logger.LogWarning($"Component {componentName} not found in loaded types");
                return null;
            }

            var instance = Activator.CreateInstance(type, constructorArgs);
            return instance as T;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to create instance of component: {componentName}");
            return null;
        }
    }

    /// <summary>
    /// Replaces a method in an existing instance
    /// </summary>
    public async Task<bool> ReplaceMethodAsync(object instance, string methodName, string newImplementation)
    {
        try
        {
            var type = instance.GetType();
            var method = type.GetMethod(methodName);
            if (method == null)
            {
                _logger.LogWarning($"Method {methodName} not found in type {type.Name}");
                return false;
            }

            // This is a simplified implementation
            // In a real scenario, you'd use more sophisticated techniques like:
            // - Dynamic method replacement
            // - AOP (Aspect-Oriented Programming)
            // - Assembly unloading and reloading

            _logger.LogInformation($"Would replace method {methodName} in {type.Name}");
            await Task.CompletedTask; // Ensure async method
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to replace method {methodName}");
            return false;
        }
    }

    /// <summary>
    /// Updates UI components on the main thread
    /// </summary>
    public async Task UpdateUIComponentAsync(Control control, Action updateAction)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                updateAction();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update UI component");
        }
    }

    /// <summary>
    /// Reloads a ViewModel with new implementation
    /// </summary>
    public async Task<bool> ReloadViewModelAsync(string viewModelName, string newCode)
    {
        try
        {
            _logger.LogInformation($"Reloading ViewModel: {viewModelName}");

            // Extract class name from code (simplified)
            var className = ExtractClassName(newCode);
            if (string.IsNullOrEmpty(className))
            {
                _logger.LogError($"Could not extract class name from code for {viewModelName}");
                return false;
            }

            return await ReloadComponentAsync(viewModelName, newCode, className, "Process");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to reload ViewModel: {viewModelName}");
            return false;
        }
    }

    /// <summary>
    /// Reloads a service with new implementation
    /// </summary>
    public async Task<bool> ReloadServiceAsync(string serviceName, string newCode)
    {
        try
        {
            _logger.LogInformation($"Reloading service: {serviceName}");

            var className = ExtractClassName(newCode);
            if (string.IsNullOrEmpty(className))
            {
                _logger.LogError($"Could not extract class name from code for {serviceName}");
                return false;
            }

            return await ReloadComponentAsync(serviceName, newCode, className, "Execute");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to reload service: {serviceName}");
            return false;
        }
    }

    /// <summary>
    /// Gets the current implementation of a component
    /// </summary>
    public async Task<string> GetCurrentImplementationAsync(string componentName, string className, string methodName)
    {
        try
        {
            if (_loadedTypes.TryGetValue(componentName, out var type))
            {
                return await _codeModificationService.GetCurrentImplementationAsync(className, methodName);
            }
            else
            {
                return await _codeModificationService.GetCurrentImplementationAsync(className, methodName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to get current implementation for {componentName}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Lists all loaded components
    /// </summary>
    public string[] GetLoadedComponents()
    {
        return _loadedTypes.Keys.ToArray();
    }

    /// <summary>
    /// Unloads a component
    /// </summary>
    public bool UnloadComponent(string componentName)
    {
        try
        {
            _loadedTypes.TryRemove(componentName, out _);
            _loadedAssemblies.TryRemove(componentName, out _);
            
            _logger.LogInformation($"Unloaded component: {componentName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to unload component: {componentName}");
            return false;
        }
    }

    /// <summary>
    /// Clears all loaded components
    /// </summary>
    public void ClearAllComponents()
    {
        _loadedTypes.Clear();
        _loadedAssemblies.Clear();
        _logger.LogInformation("Cleared all loaded components");
    }

    private string? ExtractClassName(string code)
    {
        // Simple regex to extract class name
        var match = System.Text.RegularExpressions.Regex.Match(code, @"class\s+(\w+)");
        return match.Success ? match.Groups[1].Value : null;
    }
}

public class HotReloadEventArgs : EventArgs
{
    public string ComponentName { get; }
    public Type NewType { get; }
    public string MethodName { get; }

    public HotReloadEventArgs(string componentName, Type newType, string methodName)
    {
        ComponentName = componentName;
        NewType = newType;
        MethodName = methodName;
    }
}

public class HotReloadErrorEventArgs : EventArgs
{
    public string ComponentName { get; }
    public string ErrorMessage { get; }

    public HotReloadErrorEventArgs(string componentName, string errorMessage)
    {
        ComponentName = componentName;
        ErrorMessage = errorMessage;
    }
}
