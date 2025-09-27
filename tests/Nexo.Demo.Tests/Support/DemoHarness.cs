using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Nexo.Demo.Tests.Support;

/// <summary>
/// Demo harness for running Nexo scenarios and validating results
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public static partial class DemoHarness
{
    private static int _networkAttempts = 0;
    
    /// <summary>
    /// Runs a Nexo scenario with the given environment variables
    /// </summary>
    public static async Task<RunResult> RunScenarioAsync(
        string scenarioPath,
        IDictionary<string, string>? env = null)
    {
        // Reset network attempt counter
        Interlocked.Exchange(ref _networkAttempts, 0);
        
        // Set environment variables
        var originalEnv = new Dictionary<string, string?>();
        if (env != null)
        {
            foreach (var kvp in env)
            {
                originalEnv[kvp.Key] = Environment.GetEnvironmentVariable(kvp.Key);
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }

        try
        {
            // Create a temporary output directory
            var outputDir = Path.Combine(Path.GetTempPath(), $"nexo_demo_{Guid.NewGuid():N}");
            Directory.CreateDirectory(outputDir);

            // Set up configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cache:Backend"] = "Memory",
                    ["Cache:DefaultTtlSeconds"] = "3600"
                })
                .AddEnvironmentVariables()
                .Build();

            // Set up services using real Nexo services
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            services.AddHttpClient();
            
            // Add pipeline services
            services.AddTransient<IPipelineOrchestrator, MockPipelineOrchestrator>();
            
            // Add network guard for Off/Assist modes
            var mode = Environment.GetEnvironmentVariable("NEXO_AI_MODE")?.ToLowerInvariant();
            if (mode is "off" or "assist")
            {
                services.AddHttpClient("NexoDemo", client =>
                {
                    // Configure HttpClient with network guard
                }).AddHttpMessageHandler<NetworkGuardHandler>();
            }

            var serviceProvider = services.BuildServiceProvider();

            // Run scenario using actual Nexo runtime
            var result = await RunNexoScenarioWithRealServices(scenarioPath, outputDir, serviceProvider);

            return result;
        }
        finally
        {
            // Restore original environment variables
            foreach (var kvp in originalEnv)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }
    }
}

/// <summary>
/// Result of running a Nexo scenario
/// </summary>
public sealed class RunResult
{
    public bool Completed { get; init; }
    public string OutputDir { get; init; } = "";
    public string Mode { get; init; } = "";
    public string Provider { get; init; } = "";
    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
    public int NetworkAttempts { get; init; }

    public string OutputText(string relativePath) => File.ReadAllText(Path.Combine(OutputDir, relativePath));
    public byte[] OutputBytes(string relativePath) => File.ReadAllBytes(Path.Combine(OutputDir, relativePath));
    public bool HasOutput(string relativePath) => File.Exists(Path.Combine(OutputDir, relativePath));
}

/// <summary>
/// Mock pipeline orchestrator for demo testing
/// </summary>
public interface IPipelineOrchestrator
{
    Task<PipelineExecutionResult> ExecuteApplicationPipelineAsync(ApplicationPipelineRequest request, CancellationToken cancellationToken = default);
    Task<PipelineExecutionResult> ExecuteAnalysisPipelineAsync(AnalysisPipelineRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Mock implementation of pipeline orchestrator
/// </summary>
public class MockPipelineOrchestrator : IPipelineOrchestrator
{
    public Task<PipelineExecutionResult> ExecuteApplicationPipelineAsync(ApplicationPipelineRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PipelineExecutionResult
        {
            IsSuccess = true,
            ExecutionId = Guid.NewGuid().ToString(),
            Status = ExecutionStatus.Completed
        });
    }

    public Task<PipelineExecutionResult> ExecuteAnalysisPipelineAsync(AnalysisPipelineRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PipelineExecutionResult
        {
            IsSuccess = true,
            ExecutionId = Guid.NewGuid().ToString(),
            Status = ExecutionStatus.Completed
        });
    }
}

/// <summary>
/// Standalone pipeline request types for demo testing
/// </summary>
public class ApplicationPipelineRequest
{
    public string ApplicationName { get; set; } = "";
    public string ProjectPath { get; set; } = "";
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class AnalysisPipelineRequest
{
    public string ProjectPath { get; set; } = "";
    public string AnalysisType { get; set; } = "";
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class PipelineExecutionResult
{
    public bool IsSuccess { get; set; }
    public string ExecutionId { get; set; } = "";
    public ExecutionStatus Status { get; set; }
    public string ErrorMessage { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double ExecutionTimeMs => (EndTime - StartTime).TotalMilliseconds;
}

public enum ExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}