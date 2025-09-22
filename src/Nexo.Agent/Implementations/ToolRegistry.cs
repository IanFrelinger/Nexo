using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;

namespace Nexo.Agent.Implementations;

/// <summary>
/// In-memory tool registry implementation.
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly ILogger<ToolRegistry> _logger;
    private readonly Dictionary<string, ITool> _tools;
    private readonly ActivitySource _activitySource;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tools = new Dictionary<string, ITool>();
        _activitySource = new ActivitySource("Nexo.Agent");
    }

    public IReadOnlyList<ToolManifest> GetAllTools()
    {
        using var activity = _activitySource.StartActivity("GetAllTools");
        activity?.SetTag("registry.tool_count", _tools.Count);

        return _tools.Values.Select(t => t.Manifest).ToList();
    }

    public ToolManifest? GetTool(string toolId)
    {
        using var activity = _activitySource.StartActivity("GetTool");
        activity?.SetTag("registry.tool_id", toolId);

        if (_tools.TryGetValue(toolId, out var tool))
        {
            activity?.SetTag("registry.tool_found", true);
            return tool.Manifest;
        }

        activity?.SetTag("registry.tool_found", false);
        return null;
    }

    public async Task<bool> RegisterToolAsync(ITool tool, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("RegisterTool");
        activity?.SetTag("registry.tool_id", tool.Manifest.Id);
        activity?.SetTag("registry.tool_name", tool.Manifest.Name);

        try
        {
            if (_tools.ContainsKey(tool.Manifest.Id))
            {
                _logger.LogWarning("Tool {ToolId} is already registered", tool.Manifest.Id);
                activity?.SetStatus(ActivityStatusCode.Error, "Tool already registered");
                return false;
            }

            _tools[tool.Manifest.Id] = tool;
            _logger.LogInformation("Registered tool: {ToolId} - {ToolName}", tool.Manifest.Id, tool.Manifest.Name);
            activity?.SetTag("registry.registration_success", true);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering tool {ToolId}", tool.Manifest.Id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return false;
        }
    }

    public async Task<bool> UnregisterToolAsync(string toolId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("UnregisterTool");
        activity?.SetTag("registry.tool_id", toolId);

        try
        {
            if (_tools.Remove(toolId))
            {
                _logger.LogInformation("Unregistered tool: {ToolId}", toolId);
                activity?.SetTag("registry.unregistration_success", true);
                return true;
            }

            _logger.LogWarning("Tool {ToolId} was not registered", toolId);
            activity?.SetTag("registry.unregistration_success", false);
            await Task.CompletedTask;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering tool {ToolId}", toolId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return false;
        }
    }

    public bool IsToolRegistered(string toolId)
    {
        return _tools.ContainsKey(toolId);
    }

    public async Task<int> ScanAndRegisterToolsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ScanAndRegisterTools");
        activity?.SetTag("registry.scan_directory", directoryPath);

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Tool directory does not exist: {DirectoryPath}", directoryPath);
                activity?.SetStatus(ActivityStatusCode.Error, "Directory not found");
                return 0;
            }

            var registeredCount = 0;
            var dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    // In a real implementation, this would load the assembly and register tools
                    // For now, we'll just log that we found a DLL
                    _logger.LogDebug("Found tool assembly: {DllFile}", dllFile);
                    
                    // TODO: Load assembly, find ITool implementations, register them
                    // This would involve using AssemblyLoadContext and reflection
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load tool assembly: {DllFile}", dllFile);
                }
            }

            activity?.SetTag("registry.tools_registered", registeredCount);
            _logger.LogInformation("Scanned directory {DirectoryPath}, registered {Count} tools", directoryPath, registeredCount);

            await Task.CompletedTask;
            return registeredCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning tool directory: {DirectoryPath}", directoryPath);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return 0;
        }
    }

    public void Dispose()
    {
        _activitySource.Dispose();
    }
}
