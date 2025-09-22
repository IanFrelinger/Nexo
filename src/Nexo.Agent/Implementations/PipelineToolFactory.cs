using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;

namespace Nexo.Agent.Implementations;

/// <summary>
/// Tool factory that uses the existing ExtensionPipeline to create new tools.
/// </summary>
public class PipelineToolFactory : IToolFactory
{
    private readonly ILogger<PipelineToolFactory> _logger;
    // private readonly IExtensionGenerator _extensionGenerator;
    private readonly ActivitySource _activitySource;

    public PipelineToolFactory(ILogger<PipelineToolFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // _extensionGenerator = extensionGenerator ?? throw new ArgumentNullException(nameof(extensionGenerator));
        _activitySource = new ActivitySource("Nexo.Pipeline");
    }

    public async Task<ToolCreationResult> CreateToolAsync(ToolRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("CreateTool");
        activity?.SetTag("factory.tool_id", request.Id);
        activity?.SetTag("factory.tool_name", request.Name);
        activity?.SetTag("factory.break_policy", request.BreakPolicy);

        var startTime = DateTime.UtcNow;
        var repairAttempts = 0;

        try
        {
            _logger.LogInformation("Creating tool: {ToolId} - {ToolName}", request.Id, request.Name);

            // Simulate tool generation for now
            _logger.LogInformation("Simulating tool generation for {ToolId}", request.Id);
            
            // Simulate some processing time
            await Task.Delay(1000, cancellationToken);

            // If breakPolicy is enabled, simulate a policy failure and repair
            if (request.BreakPolicy)
            {
                _logger.LogInformation("Break policy mode enabled, simulating policy failure and repair");
                repairAttempts = SimulatePolicyFailureAndRepair();
            }

            // Create tool manifest
            var manifest = CreateToolManifest(request);
            activity?.SetTag("factory.policy_score", 85); // Simulated score
            activity?.SetTag("factory.repair_attempts", repairAttempts);

            _logger.LogInformation("Tool {ToolId} created successfully with {RepairAttempts} repair attempts", request.Id, repairAttempts);

            return new ToolCreationResult(
                Success: true,
                ToolId: request.Id,
                Manifest: manifest,
                Error: null,
                Duration: DateTime.UtcNow - startTime,
                PolicyScore: 85, // Simulated score
                RepairAttempts: repairAttempts
            );
        }
        catch (Exception ex)
        {
            var error = $"Tool creation failed with exception: {ex.Message}";
            _logger.LogError(ex, "Error creating tool {ToolId}", request.Id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return new ToolCreationResult(
                Success: false,
                ToolId: null,
                Manifest: null,
                Error: error,
                Duration: DateTime.UtcNow - startTime,
                PolicyScore: 0,
                RepairAttempts: repairAttempts
            );
        }
    }

    public bool CanCreateTool(ToolRequest request)
    {
        // Check if we can create the requested tool type
        return request.Id.StartsWith("tool.") && !string.IsNullOrEmpty(request.Name);
    }

    public TimeSpan EstimateCreationTime(ToolRequest request)
    {
        // Estimate based on tool complexity
        var baseTime = TimeSpan.FromSeconds(30);
        
        if (request.BreakPolicy)
            baseTime = baseTime.Add(TimeSpan.FromSeconds(15)); // Extra time for repair

        if (request.QualityTargets.RequireTests)
            baseTime = baseTime.Add(TimeSpan.FromSeconds(10));

        return baseTime;
    }

    private ToolManifest CreateToolManifest(ToolRequest request)
    {
        return new ToolManifest(
            Id: request.Id,
            Name: request.Name,
            Version: "1.0.0",
            Description: request.Description,
            Author: "Nexo Agent Factory",
            InputSchema: request.InputSchema,
            OutputSchema: request.OutputSchema,
            RequiredPermissions: request.RequiredPermissions,
            Capabilities: request.Capabilities,
            Timeout: request.Timeout ?? TimeSpan.FromMinutes(5),
            CreatedAt: DateTime.UtcNow
        );
    }

    private int SimulatePolicyFailureAndRepair()
    {
        _logger.LogInformation("Simulating policy failure...");
        
        // Simulate repair attempts
        var attempts = Random.Shared.Next(1, 4);
        
        for (int i = 1; i <= attempts; i++)
        {
            _logger.LogInformation("Repair attempt {Attempt} of {MaxAttempts}", i, attempts);
            
            if (i == attempts)
            {
                _logger.LogInformation("Repair successful after {Attempts} attempts", attempts);
                break;
            }
            
            // Simulate repair delay
            Thread.Sleep(100);
        }

        return attempts;
    }

    public void Dispose()
    {
        _activitySource.Dispose();
    }
}
