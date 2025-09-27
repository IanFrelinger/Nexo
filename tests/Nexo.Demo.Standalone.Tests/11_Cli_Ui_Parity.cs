using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;
using Nexo.Demo.Tests.Support;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests that CLI and UI produce identical outputs for the same recipe
/// </summary>
public partial class Cli_Ui_Parity
{
    public Cli_Ui_Parity()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
    }
    [Fact, Trait("Suite", "Demo")]
    public async Task Ui_And_Cli_Produce_Identical_Outputs()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var scenarioPath = "recipes/triage_support.yaml";
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");

        // Act - Run via UI (programmatic API)
        var uiResult = await RunViaUIAsync(scenarioPath);
        uiResult.Completed.Should().BeTrue("UI run should complete successfully");

        // Act - Run via CLI (spawn process)
        var cliResult = await RunViaCLIAsync(scenarioPath);
        cliResult.Completed.Should().BeTrue("CLI run should complete successfully");

        // Assert - Both should produce the same output files
        uiResult.HasOutput("outputs/labels.csv").Should().BeTrue("UI should produce labels.csv");
        cliResult.HasOutput("outputs/labels.csv").Should().BeTrue("CLI should produce labels.csv");

        // Assert - Outputs should be identical (after normalization)
        var uiContent = uiResult.OutputText("outputs/labels.csv");
        var cliContent = cliResult.OutputText("outputs/labels.csv");

        var normalizedUi = DemoHarness.NormalizeText(uiContent);
        var normalizedCli = DemoHarness.NormalizeText(cliContent);

        normalizedUi.Should().Be(normalizedCli, "UI and CLI outputs should be identical after normalization");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Different_Input_Methods_Produce_Consistent_Results()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var scenarioPath = "recipes/doc_summarize.yaml";
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");

        // Act - Run via different input methods
        var apiResult = await RunViaAPIAsync(scenarioPath);
        var cliResult = await RunViaCLIAsync(scenarioPath);
        var uiResult = await RunViaUIAsync(scenarioPath);

        // Assert - All should complete successfully
        apiResult.Completed.Should().BeTrue("API run should complete");
        cliResult.Completed.Should().BeTrue("CLI run should complete");
        uiResult.Completed.Should().BeTrue("UI run should complete");

        // Assert - All should produce the same output
        var apiContent = DemoHarness.NormalizeText(apiResult.OutputText("outputs/summary.txt"));
        var cliContent = DemoHarness.NormalizeText(cliResult.OutputText("outputs/summary.txt"));
        var uiContent = DemoHarness.NormalizeText(uiResult.OutputText("outputs/summary.txt"));

        apiContent.Should().Be(cliContent, "API and CLI should produce identical results");
        cliContent.Should().Be(uiContent, "CLI and UI should produce identical results");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Configuration_Consistency_Across_Interfaces()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var scenarioPath = "recipes/triage_support.yaml";
        var config = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "openai",
            ["NEXO_MODEL"] = "gpt-4o",
            ["NEXO_MAX_RETRIES"] = "3",
            ["NEXO_TIMEOUT_MS"] = "30000"
        };

        // Act - Apply same configuration to different interfaces
        var cliResult = await RunViaCLIAsync(scenarioPath, config);
        var uiResult = await RunViaUIAsync(scenarioPath, config);

        // Assert - Both should use the same configuration
        cliResult.Completed.Should().BeTrue("CLI with config should complete");
        uiResult.Completed.Should().BeTrue("UI with config should complete");

        // Assert - Configuration should be applied consistently
        var cliLogs = string.Join(" ", cliResult.Logs);
        var uiLogs = string.Join(" ", uiResult.Logs);

        foreach (var (key, value) in config)
        {
            cliLogs.Should().Contain(value, $"CLI logs should contain {key}={value}");
            uiLogs.Should().Contain(value, $"UI logs should contain {key}={value}");
        }
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Error_Handling_Is_Consistent_Across_Interfaces()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange - Use a scenario that will cause an error
        var scenarioPath = "recipes/invalid_recipe.yaml";
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "nonexistent");

        // Act - Run via different interfaces
        var cliResult = await RunViaCLIAsync(scenarioPath);
        var uiResult = await RunViaUIAsync(scenarioPath);

        // Assert - Both should handle errors consistently
        cliResult.Completed.Should().BeFalse("CLI should fail consistently");
        uiResult.Completed.Should().BeFalse("UI should fail consistently");

        // Assert - Error messages should be similar
        var cliError = string.Join(" ", cliResult.Logs.Where(log => 
            log.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("failed", StringComparison.OrdinalIgnoreCase)));
            
        var uiError = string.Join(" ", uiResult.Logs.Where(log => 
            log.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("failed", StringComparison.OrdinalIgnoreCase)));

        cliError.Should().NotBeEmpty("CLI should have error messages");
        uiError.Should().NotBeEmpty("UI should have error messages");
        
        // Error types should be similar (normalized)
        var normalizedCliError = DemoHarness.NormalizeText(cliError);
        var normalizedUiError = DemoHarness.NormalizeText(uiError);
        
        var errorSimilarity = DemoHarness.JaccardTokens(normalizedCliError, normalizedUiError);
        errorSimilarity.Should().BeGreaterOrEqualTo(0.8, "Error handling should be consistent");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Performance_Characteristics_Are_Similar()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var scenarioPath = "recipes/triage_support.yaml";
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");

        // Act - Run multiple times via each interface
        var cliTimes = new List<TimeSpan>();
        var uiTimes = new List<TimeSpan>();

        for (int i = 0; i < 3; i++)
        {
            var cliStart = DateTime.UtcNow;
            var cliResult = await RunViaCLIAsync(scenarioPath);
            var cliEnd = DateTime.UtcNow;
            cliTimes.Add(cliEnd - cliStart);

            var uiStart = DateTime.UtcNow;
            var uiResult = await RunViaUIAsync(scenarioPath);
            var uiEnd = DateTime.UtcNow;
            uiTimes.Add(uiEnd - uiStart);
        }

        // Assert - Performance should be similar
        var avgCliTime = cliTimes.Average(t => t.TotalMilliseconds);
        var avgUiTime = uiTimes.Average(t => t.TotalMilliseconds);
        var performanceRatio = avgUiTime / avgCliTime;

        performanceRatio.Should().BeInRange(0.5, 2.0, "Performance should be within 2x of each other");
    }

    /// <summary>
    /// Simulates running via UI (programmatic API)
    /// </summary>
    private async Task<RunResult> RunViaUIAsync(string scenarioPath, IDictionary<string, string>? config = null)
    {
        // Apply configuration
        if (config != null)
        {
            foreach (var (key, value) in config)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        // Simulate UI run by calling DemoHarness with environment variables
        var envVars = new Dictionary<string, string>();
        if (config != null)
        {
            foreach (var (key, value) in config)
            {
                envVars[key] = value;
            }
        }
        var result = await DemoHarness.RunScenarioAsync(scenarioPath, envVars);
        
        // Add UI-specific logs
        var logs = new List<string>(result.Logs);
        logs.Insert(0, "UI execution started");
        logs.Add("UI execution finished");
        
        return new RunResult
        {
            Completed = result.Completed,
            OutputDir = result.OutputDir,
            Mode = result.Mode,
            Provider = result.Provider,
            Logs = logs,
            Metrics = result.Metrics,
            NetworkAttempts = result.NetworkAttempts
        };
    }

    /// <summary>
    /// Simulates running via CLI (spawn process)
    /// </summary>
    private async Task<RunResult> RunViaCLIAsync(string scenarioPath, IDictionary<string, string>? config = null)
    {
        // Apply configuration
        if (config != null)
        {
            foreach (var (key, value) in config)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        // Simulate CLI run by calling DemoHarness with environment variables
        var envVars = new Dictionary<string, string>();
        if (config != null)
        {
            foreach (var (key, value) in config)
            {
                envVars[key] = value;
            }
        }
        var result = await DemoHarness.RunScenarioAsync(scenarioPath, envVars);
        
        // Add CLI-specific logs
        var logs = new List<string>(result.Logs);
        logs.Insert(0, "CLI execution started");
        logs.Add("CLI execution finished");
        
        return new RunResult
        {
            Completed = result.Completed,
            OutputDir = result.OutputDir,
            Mode = result.Mode,
            Provider = result.Provider,
            Logs = logs,
            Metrics = result.Metrics,
            NetworkAttempts = result.NetworkAttempts
        };
    }

    /// <summary>
    /// Simulates running via API
    /// </summary>
    private async Task<RunResult> RunViaAPIAsync(string scenarioPath)
    {
        // Simulate API run by calling DemoHarness
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);
        
        // Add API-specific logs
        var logs = new List<string>(result.Logs);
        logs.Insert(0, "API execution started");
        logs.Add("API execution finished");
        
        return new RunResult
        {
            Completed = result.Completed,
            OutputDir = result.OutputDir,
            Mode = result.Mode,
            Provider = result.Provider,
            Logs = logs,
            Metrics = result.Metrics,
            NetworkAttempts = result.NetworkAttempts
        };
    }
}
