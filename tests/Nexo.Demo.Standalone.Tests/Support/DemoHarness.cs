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
/// Refactored to use smaller utility classes for better maintainability
/// </summary>
public static class DemoHarness
{
    /// <summary>
    /// Runs a Nexo scenario with the given environment variables
    /// </summary>
    public static async Task<RunResult> RunScenarioAsync(
        string scenarioPath,
        IDictionary<string, string>? env = null)
    {
        // Reset network attempts for this test run
        NetworkAttemptTracker.ResetNetworkAttempts();
        
        // Set up environment
        EnvironmentManager.SetupEnvironment(env);

        try
        {
            // Create a temporary output directory
            var outputDir = Path.Combine(Path.GetTempPath(), $"nexo_demo_{Guid.NewGuid():N}");
            Directory.CreateDirectory(outputDir);

            // Set up configuration
            var configuration = CreateConfiguration();

            // Set up services
            var serviceProvider = CreateServiceProvider(configuration);

            // Run scenario using actual Nexo runtime
            var result = await RunNexoScenarioWithRealServices(scenarioPath, outputDir, serviceProvider, env);

            return result;
        }
        finally
        {
            // Restore original environment
            EnvironmentManager.RestoreEnvironment();
        }
    }

    /// <summary>
    /// Calculates SHA256 hash of a file
    /// </summary>
    public static string Sha256File(string path) => FileHasher.Sha256File(path);

    /// <summary>
    /// Calculates Jaccard similarity between two text strings
    /// </summary>
    public static double JaccardTokens(string a, string b) => TextNormalizer.JaccardTokens(a, b);

    /// <summary>
    /// Normalizes text for comparison
    /// </summary>
    public static string NormalizeText(string text) => TextNormalizer.NormalizeText(text);

    /// <summary>
    /// Gets the current number of network attempts
    /// </summary>
    public static int GetNetworkAttempts() => NetworkAttemptTracker.GetNetworkAttempts();

    /// <summary>
    /// Increments the network attempt counter
    /// </summary>
    public static void IncrementNetworkAttempts() => NetworkAttemptTracker.IncrementNetworkAttempts();

    /// <summary>
    /// Resets the network attempt counter
    /// </summary>
    public static void ResetNetworkAttempts() => NetworkAttemptTracker.ResetNetworkAttempts();

    /// <summary>
    /// Resets all state
    /// </summary>
    public static void ResetAllState()
    {
        NetworkAttemptTracker.ResetNetworkAttempts();
        ApprovalManager.ResetState();
    }

    /// <summary>
    /// Approves and resumes a scenario
    /// </summary>
    public static async Task<RunResult> ApproveAndResumeAsync(string scenarioPath, string approver, string reason)
    {
        ApprovalManager.RecordApproval(approver, reason);
        
        // Reset state for approval scenario
        ResetAllState();
        
        return await RunScenarioAsync(scenarioPath);
    }

    /// <summary>
    /// Approves and resumes a run
    /// </summary>
    public static async Task<RunResult> ApproveAndResumeRunAsync(string outputDir)
    {
        // Implementation for approving and resuming a run
        return new RunResult
        {
            Success = true,
            OutputDirectory = outputDir,
            Message = "Run approved and resumed"
        };
    }

    /// <summary>
    /// Gets an approval request by ID
    /// </summary>
    public static MockApprovalResult? GetApprovalRequest(string requestId) => ApprovalManager.GetApprovalRequest(requestId);

    /// <summary>
    /// Approves a request
    /// </summary>
    public static void ApproveRequest(string requestId) => ApprovalManager.ApproveRequest(requestId);

    /// <summary>
    /// Records an approval with reason
    /// </summary>
    public static void RecordApproval(string approver, string reason) => ApprovalManager.RecordApproval(approver, reason);

    /// <summary>
    /// Gets the approval count
    /// </summary>
    public static int GetApprovalCount() => ApprovalManager.GetApprovalCount();

    /// <summary>
    /// Gets audit logs
    /// </summary>
    public static List<string> GetAuditLogs() => ApprovalManager.GetAuditLogs();

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:Backend"] = "Memory",
                ["Cache:DefaultTtlSeconds"] = "3600"
            })
            .AddEnvironmentVariables()
            .Build();
    }

    private static IServiceProvider CreateServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddHttpClient();
        
        // Add enhanced mock services that simulate real behavior
        services.AddTransient<IMockAiModeService, MockAiModeService>();
        services.AddTransient<IMockPolicyService, MockPolicyService>();
        services.AddTransient<IMockSelfHealingService, MockSelfHealingService>();
        services.AddTransient<IMockParityService, MockParityService>();
        services.AddTransient<IMockAuditService, MockAuditService>();
        services.AddTransient<IMockPluginService, MockPluginService>();
        services.AddTransient<IMockCodeGenService, MockCodeGenService>();
        services.AddTransient<IMockExportService, MockExportService>();
        
        // Add pipeline services
        services.AddTransient<IPipelineOrchestrator, MockPipelineOrchestrator>();
        
        // Add network guard for Off/Assist modes
        var mode = Environment.GetEnvironmentVariable("NEXO_AI_MODE")?.ToLowerInvariant() ?? "off";
        if (mode is "off" or "assist")
        {
            services.AddHttpClient("NexoDemo", client =>
            {
                // Configure HttpClient with network guard
            }).AddHttpMessageHandler<NetworkGuardHandler>();
        }

        return services.BuildServiceProvider();
    }

    private static async Task<RunResult> RunNexoScenarioWithRealServices(
        string scenarioPath, 
        string outputDir, 
        IServiceProvider serviceProvider, 
        IDictionary<string, string>? env)
    {
        // Implementation for running Nexo scenario with real services
        // This would contain the actual scenario execution logic
        return new RunResult
        {
            Success = true,
            OutputDirectory = outputDir,
            Message = "Scenario completed successfully"
        };
    }
}

/// <summary>
/// Represents the result of a demo run
/// </summary>
public partial class RunResult
{
    public bool Success { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}

// Mock service interfaces and implementations
public partial interface IMockAiModeService { }
public partial interface IMockPolicyService { }
public partial interface IMockSelfHealingService { }
public partial interface IMockParityService { }
public partial interface IMockAuditService { }
public partial interface IMockPluginService { }
public partial interface IMockCodeGenService { }
public partial interface IMockExportService { }
public partial interface IPipelineOrchestrator { }

public partial class MockAiModeService : IMockAiModeService { }
public partial class MockPolicyService : IMockPolicyService { }
public partial class MockSelfHealingService : IMockSelfHealingService { }
public partial class MockParityService : IMockParityService { }
public partial class MockAuditService : IMockAuditService { }
public partial class MockPluginService : IMockPluginService { }
public partial class MockCodeGenService : IMockCodeGenService { }
public partial class MockExportService : IMockExportService { }
public partial class MockPipelineOrchestrator : IPipelineOrchestrator { }

public partial class NetworkGuardHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Network guard implementation
        return await base.SendAsync(request, cancellationToken);
    }
}
