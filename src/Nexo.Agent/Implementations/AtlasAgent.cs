using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;
using Nexo.Observability.ActivitySources;

namespace Nexo.Agent.Implementations;

/// <summary>
/// Default implementation of the Atlas agent.
/// </summary>
public class AtlasAgent : ITaskExecutionAgent
{
    private readonly ILogger<AtlasAgent> _logger;
    private readonly IAgentPlanner _planner;
    private readonly IToolBroker _toolBroker;
    private readonly IToolRegistry _toolRegistry;
    private readonly IToolFactory _toolFactory;
    private readonly ActivitySource _activitySource;
    private AgentStatus _status = AgentStatus.Idle;
    private AgentMode _mode = AgentMode.Off;

    public AtlasAgent(
        ILogger<AtlasAgent> logger,
        IAgentPlanner planner,
        IToolBroker toolBroker,
        IToolRegistry toolRegistry,
        IToolFactory toolFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _toolBroker = toolBroker ?? throw new ArgumentNullException(nameof(toolBroker));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _toolFactory = toolFactory ?? throw new ArgumentNullException(nameof(toolFactory));
        _activitySource = new ActivitySource("Nexo.Agent");
    }

    public AgentStatus Status => _status;
    public AgentMode Mode => _mode;

    public void SetMode(AgentMode mode)
    {
        _mode = mode;
        _logger.LogInformation("Agent mode changed to {Mode}", mode);
    }

    public async Task<AgentResult> ExecuteTaskAsync(string goal, string? context = null, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ExecuteTask");
        activity?.SetTag("agent.goal", goal);
        activity?.SetTag("agent.mode", _mode.ToString());
        activity?.SetTag("agent.context", context ?? "");

        var startTime = DateTime.UtcNow;
        var outputs = new List<string>();

        try
        {
            _status = AgentStatus.Planning;
            _logger.LogInformation("Starting task execution: {Goal}", goal);

            // Get available tools
            var availableTools = _toolRegistry.GetAllTools();
            activity?.SetTag("agent.available_tools", availableTools.Count);

            // Create plan
            var plan = await _planner.CreatePlanAsync(goal, context, availableTools, _mode, cancellationToken);
            activity?.SetTag("agent.plan_steps", plan.StepCount);

            if (!plan.HasSteps)
            {
                _status = AgentStatus.Idle;
                return new AgentResult(false, "No steps could be planned for this goal", outputs, plan, DateTime.UtcNow - startTime);
            }

            // Check for missing tools
            var missingTools = _planner.IdentifyMissingTools(plan, availableTools);
            if (missingTools.Count > 0)
            {
                _logger.LogInformation("Missing {Count} tools, requesting creation", missingTools.Count);
                activity?.SetTag("agent.missing_tools", missingTools.Count);

                // For now, we'll fail if tools are missing
                // In a real implementation, this would trigger tool creation
                _status = AgentStatus.Error;
                return new AgentResult(false, $"Missing required tools: {string.Join(", ", missingTools.Select(t => t.Id))}", outputs, plan, DateTime.UtcNow - startTime);
            }

            // Execute plan
            _status = AgentStatus.Executing;
            var executionResult = await ExecutePlanAsync(plan, outputs, cancellationToken);

            _status = executionResult.Success ? AgentStatus.Idle : AgentStatus.Error;
            return new AgentResult(executionResult.Success, executionResult.Message, outputs, plan, DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            _status = AgentStatus.Error;
            _logger.LogError(ex, "Error executing task: {Goal}", goal);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new AgentResult(false, $"Error: {ex.Message}", outputs, null, DateTime.UtcNow - startTime);
        }
    }

    private async Task<(bool Success, string Message)> ExecutePlanAsync(Plan plan, List<string> outputs, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("ExecutePlan");
        activity?.SetTag("plan.id", plan.Id);
        activity?.SetTag("plan.steps", plan.StepCount);

        var completedSteps = new HashSet<int>();
        var stepResults = new Dictionary<int, string>();

        foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
        {
            // Check dependencies
            if (step.DependsOn.Any(dep => !completedSteps.Contains(dep)))
            {
                return (false, $"Step {step.StepNumber} has unmet dependencies");
            }

            try
            {
                _logger.LogInformation("Executing step {StepNumber}: {Description}", step.StepNumber, step.Description);

                // Create tool context
                var toolContext = new ToolContext(
                    Logger: _logger,
                    CancellationToken: cancellationToken,
                    Timeout: step.Timeout ?? TimeSpan.FromMinutes(5),
                    Permissions: ToolPermissions.All, // In a real implementation, this would be more restrictive
                    FileSystem: new DefaultFileSystem(),
                    WorkingDirectory: Environment.CurrentDirectory
                );

                // Execute tool
                var result = await _toolBroker.ExecuteToolAsync(step.ToolId, step.Input, toolContext, cancellationToken);

                if (!result.Success)
                {
                    return (false, $"Step {step.StepNumber} failed: {result.Error}");
                }

                stepResults[step.StepNumber] = result.Output ?? "";
                completedSteps.Add(step.StepNumber);

                if (!string.IsNullOrEmpty(result.Output))
                {
                    outputs.Add(result.Output);
                }

                _logger.LogInformation("Step {StepNumber} completed successfully", step.StepNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing step {StepNumber}", step.StepNumber);
                return (false, $"Step {step.StepNumber} failed with exception: {ex.Message}");
            }
        }

        return (true, $"Plan executed successfully with {completedSteps.Count} steps");
    }

    public void Dispose()
    {
        _activitySource.Dispose();
    }
}
