using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nexo.Demo.Tests.Support;

/// <summary>
/// Scenario execution functionality for demo harness.
/// </summary>
public static partial class DemoHarness
{
    /// <summary>
    /// Runs a Nexo scenario using real Nexo services
    /// </summary>
    private static async Task<RunResult> RunNexoScenarioWithRealServices(
        string scenarioPath,
        string outputDir,
        IServiceProvider serviceProvider)
    {
        var logs = new List<string>();
        var metrics = new Dictionary<string, double>();
        var startTime = DateTime.UtcNow;

        try
        {
            // Get the pipeline orchestrator
            var orchestrator = serviceProvider.GetRequiredService<IPipelineOrchestrator>();
            var logger = serviceProvider.GetRequiredService<ILogger<DemoHarness>>();
            
            // Determine AI mode and create appropriate request
            var mode = Environment.GetEnvironmentVariable("NEXO_AI_MODE")?.ToLowerInvariant() ?? "off";
            var provider = Environment.GetEnvironmentVariable("NEXO_PROVIDER") ?? "local";
            
            logs.Add($"Executing scenario in {mode} mode with {provider} provider");
            
            // Create pipeline request based on scenario type
            var request = CreatePipelineRequest(scenarioPath, mode, provider);
            
            // Execute the pipeline
            PipelineExecutionResult result;
            if (request is ApplicationPipelineRequest appRequest)
            {
                result = await orchestrator.ExecuteApplicationPipelineAsync(appRequest);
            }
            else if (request is AnalysisPipelineRequest analysisRequest)
            {
                result = await orchestrator.ExecuteAnalysisPipelineAsync(analysisRequest);
            }
            else
            {
                throw new NotSupportedException($"Unsupported pipeline request type: {request.GetType()}");
            }

            // Generate outputs based on mode
            await GenerateScenarioOutputs(scenarioPath, outputDir, mode, provider, logs, metrics);

            var duration = DateTime.UtcNow - startTime;
            metrics["duration_ms"] = duration.TotalMilliseconds;
            metrics["ai_calls"] = mode is "off" or "assist" ? 0 : 1;
            metrics["network_attempts"] = GetNetworkAttempts();

            logs.Add($"Scenario completed successfully in {duration.TotalMilliseconds:F0}ms");

            return new RunResult
            {
                Completed = result.IsSuccess,
                OutputDir = outputDir,
                Mode = mode,
                Provider = provider,
                Logs = logs,
                Metrics = metrics,
                NetworkAttempts = GetNetworkAttempts()
            };
        }
        catch (Exception ex)
        {
            logs.Add($"Error during scenario execution: {ex.Message}");
            return new RunResult
            {
                Completed = false,
                OutputDir = outputDir,
                Logs = logs,
                Metrics = metrics,
                NetworkAttempts = GetNetworkAttempts()
            };
        }
    }

    /// <summary>
    /// Creates a pipeline request based on the scenario path and mode
    /// </summary>
    private static object CreatePipelineRequest(string scenarioPath, string mode, string provider)
    {
        // For demo purposes, create different request types based on scenario
        if (scenarioPath.Contains("triage") || scenarioPath.Contains("labels"))
        {
            return new ApplicationPipelineRequest
            {
                ApplicationName = "DemoTriageApp",
                ProjectPath = scenarioPath,
                Configuration = new Dictionary<string, object>
                {
                    ["ai_mode"] = mode,
                    ["provider"] = provider
                }
            };
        }
        else if (scenarioPath.Contains("summary") || scenarioPath.Contains("analysis"))
        {
            return new AnalysisPipelineRequest
            {
                ProjectPath = scenarioPath,
                AnalysisType = "DocumentAnalysis",
                Configuration = new Dictionary<string, object>
                {
                    ["ai_mode"] = mode,
                    ["provider"] = provider
                }
            };
        }
        else
        {
            return new ApplicationPipelineRequest
            {
                ApplicationName = "DemoApp",
                ProjectPath = scenarioPath,
                Configuration = new Dictionary<string, object>
                {
                    ["ai_mode"] = mode,
                    ["provider"] = provider
                }
            };
        }
    }
}
