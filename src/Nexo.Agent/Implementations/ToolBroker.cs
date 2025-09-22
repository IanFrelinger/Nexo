using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;

namespace Nexo.Agent.Implementations;

/// <summary>
/// Tool broker that executes tools with proper permissions and observability.
/// </summary>
public class ToolBroker : IToolBroker
{
    private readonly ILogger<ToolBroker> _logger;
    private readonly IToolRegistry _toolRegistry;
    private readonly ActivitySource _activitySource;

    public ToolBroker(ILogger<ToolBroker> logger, IToolRegistry toolRegistry)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _activitySource = new ActivitySource("Nexo.Tool");
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(
        string toolId,
        string input,
        ToolContext context,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ExecuteTool");
        activity?.SetTag("tool.id", toolId);
        activity?.SetTag("tool.input_length", input.Length);

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Executing tool {ToolId}", toolId);

            // Get tool manifest
            var manifest = _toolRegistry.GetTool(toolId);
            if (manifest == null)
            {
                var error = $"Tool {toolId} not found";
                _logger.LogError(error);
                activity?.SetStatus(ActivityStatusCode.Error, error);
                return new ToolExecutionResult(false, null, error, DateTime.UtcNow - startTime, toolId);
            }

            // Check permissions
            if (!manifest.HasRequiredPermissions(context.Permissions))
            {
                var error = $"Insufficient permissions for tool {toolId}. Required: {manifest.RequiredPermissions}, Available: {context.Permissions}";
                _logger.LogError(error);
                activity?.SetStatus(ActivityStatusCode.Error, error);
                return new ToolExecutionResult(false, null, error, DateTime.UtcNow - startTime, toolId);
            }

            // Execute tool
            var output = await ExecuteToolInternalAsync(toolId, input, context, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            activity?.SetTag("tool.success", true);
            activity?.SetTag("tool.duration_ms", duration.TotalMilliseconds);
            activity?.SetTag("tool.output_length", output?.Length ?? 0);

            _logger.LogInformation("Tool {ToolId} executed successfully in {Duration}ms", toolId, duration.TotalMilliseconds);

            return new ToolExecutionResult(true, output, null, duration, toolId);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            var error = $"Tool execution failed: {ex.Message}";
            
            _logger.LogError(ex, "Error executing tool {ToolId}", toolId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("tool.success", false);
            activity?.SetTag("tool.duration_ms", duration.TotalMilliseconds);

            return new ToolExecutionResult(false, null, error, duration, toolId);
        }
    }

    public async Task<ToolExecutionResult<TOutput>> ExecuteToolAsync<TInput, TOutput>(
        string toolId,
        TInput input,
        ToolContext context,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ExecuteToolTyped");
        activity?.SetTag("tool.id", toolId);
        activity?.SetTag("tool.input_type", typeof(TInput).Name);
        activity?.SetTag("tool.output_type", typeof(TOutput).Name);

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Executing typed tool {ToolId}<{InputType}, {OutputType}>", toolId, typeof(TInput).Name, typeof(TOutput).Name);

            // Get tool manifest
            var manifest = _toolRegistry.GetTool(toolId);
            if (manifest == null)
            {
                var error = $"Tool {toolId} not found";
                _logger.LogError(error);
                activity?.SetStatus(ActivityStatusCode.Error, error);
                return new ToolExecutionResult<TOutput>(false, default, error, DateTime.UtcNow - startTime, toolId);
            }

            // Check permissions
            if (!manifest.HasRequiredPermissions(context.Permissions))
            {
                var error = $"Insufficient permissions for tool {toolId}";
                _logger.LogError(error);
                activity?.SetStatus(ActivityStatusCode.Error, error);
                return new ToolExecutionResult<TOutput>(false, default, error, DateTime.UtcNow - startTime, toolId);
            }

            // Execute tool
            var output = await ExecuteTypedToolInternalAsync<TInput, TOutput>(toolId, input, context, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            activity?.SetTag("tool.success", true);
            activity?.SetTag("tool.duration_ms", duration.TotalMilliseconds);

            _logger.LogInformation("Typed tool {ToolId} executed successfully in {Duration}ms", toolId, duration.TotalMilliseconds);

            return new ToolExecutionResult<TOutput>(true, output, null, duration, toolId);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            var error = $"Typed tool execution failed: {ex.Message}";
            
            _logger.LogError(ex, "Error executing typed tool {ToolId}", toolId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("tool.success", false);
            activity?.SetTag("tool.duration_ms", duration.TotalMilliseconds);

            return new ToolExecutionResult<TOutput>(false, default, error, duration, toolId);
        }
    }

    public bool CanExecuteTool(string toolId, ToolPermissions permissions)
    {
        var manifest = _toolRegistry.GetTool(toolId);
        if (manifest == null)
            return false;

        return manifest.HasRequiredPermissions(permissions);
    }

    private async Task<string?> ExecuteToolInternalAsync(string toolId, string input, ToolContext context, CancellationToken cancellationToken)
    {
        // In a real implementation, this would get the actual tool instance and execute it
        // For now, we'll simulate tool execution based on the tool ID
        
        _logger.LogDebug("Simulating execution of tool {ToolId} with input: {Input}", toolId, input);

        // Simulate some processing time
        await Task.Delay(100, cancellationToken);

        return toolId switch
        {
            "tool.file.read" => "{\"content\": \"File content here\", \"size\": 1024}",
            "tool.csv.query" => "{\"rows\": 10, \"columns\": 5, \"data\": []}",
            "tool.report.write" => "{\"success\": true, \"path\": \"out/report.md\"}",
            "tool.text.summarize" => "{\"summary\": \"This is a summary of the content.\"}",
            "tool.archive.zip" => "{\"success\": true, \"path\": \"out/package.zip\"}",
            _ => "{\"result\": \"Tool executed successfully\"}"
        };
    }

    private async Task<TOutput?> ExecuteTypedToolInternalAsync<TInput, TOutput>(string toolId, TInput input, ToolContext context, CancellationToken cancellationToken)
    {
        // In a real implementation, this would get the actual typed tool instance and execute it
        // For now, we'll simulate execution
        
        _logger.LogDebug("Simulating typed execution of tool {ToolId}", toolId);

        // Simulate some processing time
        await Task.Delay(100, cancellationToken);

        // Return default value for now
        return default(TOutput);
    }

    public void Dispose()
    {
        _activitySource.Dispose();
    }
}
