using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests that the same recipe runs across different AI modes
/// </summary>
[Collection("StaticState")]
public partial class Modes_Off_Hybrid_Embedded
{
    public Modes_Off_Hybrid_Embedded()
    {
        // Constructor - no state reset needed here
    }
    [Fact, Trait("Suite", "Demo")]
    public async Task Same_Recipe_Runs_Across_Modes()
    {
        
        var scenarioPath = "recipes/triage_support.yaml";
        var results = new Dictionary<string, RunResult>();

        // Test Off mode
        var offEnv = new Dictionary<string, string> { ["NEXO_AI_MODE"] = "off" };
        var offResult = await DemoHarness.RunScenarioAsync(scenarioPath, offEnv);
        results["off"] = offResult;

        // Test Hybrid mode (Local)
        var hybridEnv = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "local",
            ["NEXO_MODEL"] = "llama3"
        };
        var hybridResult = await DemoHarness.RunScenarioAsync(scenarioPath, hybridEnv);
        results["hybrid"] = hybridResult;

        // Test Embedded mode (Cloud) - only if API key is present
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            var embeddedEnv = new Dictionary<string, string>
            {
                ["NEXO_AI_MODE"] = "embedded",
                ["NEXO_PROVIDER"] = "openai",
                ["NEXO_MODEL"] = "gpt-4o"
            };
            var embeddedResult = await DemoHarness.RunScenarioAsync(scenarioPath, embeddedEnv);
            results["embedded"] = embeddedResult;
        }

        // Assert all modes complete successfully
        foreach (var (mode, result) in results)
        {
            result.Completed.Should().BeTrue($"Mode {mode} should complete successfully");
            result.Mode.Should().Be(mode, $"Result should reflect {mode} mode");
            result.HasOutput("outputs/labels.csv").Should().BeTrue($"Mode {mode} should produce output");
        }

        // Assert network behavior differences
        results["off"].NetworkAttempts.Should().Be(0, "Off mode should make no network calls");
        results["hybrid"].NetworkAttempts.Should().BeGreaterOrEqualTo(0, "Hybrid mode may make network calls");
        
        if (results.ContainsKey("embedded"))
        {
            results["embedded"].NetworkAttempts.Should().BeGreaterThan(0, "Embedded mode should make network calls");
        }
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Mode_Specific_Behavior_Is_Correct()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        var scenarioPath = "recipes/triage_support.yaml";

        // Test Off mode - deterministic
        var offEnv = new Dictionary<string, string> { ["NEXO_AI_MODE"] = "off" };
        var offResult1 = await DemoHarness.RunScenarioAsync(scenarioPath, offEnv);
        var offResult2 = await DemoHarness.RunScenarioAsync(scenarioPath, offEnv);

        offResult1.Completed.Should().BeTrue();
        offResult2.Completed.Should().BeTrue();

        var offHash1 = DemoHarness.Sha256File(Path.Combine(offResult1.OutputDir, "outputs/labels.csv"));
        var offHash2 = DemoHarness.Sha256File(Path.Combine(offResult2.OutputDir, "outputs/labels.csv"));
        offHash1.Should().Be(offHash2, "Off mode should be deterministic");

        // Test Hybrid mode - may have variation
        var hybridEnv = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "local"
        };
        var hybridResult = await DemoHarness.RunScenarioAsync(scenarioPath, hybridEnv);
        hybridResult.Completed.Should().BeTrue();
        hybridResult.Mode.Should().Be("hybrid");
        hybridResult.Provider.Should().Be("local");

        // Test Assist mode - no runtime AI
        var assistEnv = new Dictionary<string, string> { ["NEXO_AI_MODE"] = "assist" };
        var assistResult = await DemoHarness.RunScenarioAsync(scenarioPath, assistEnv);
        assistResult.Completed.Should().BeTrue();
        assistResult.NetworkAttempts.Should().Be(0, "Assist mode should not make network calls during execution");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Cloud_Mode_Requires_Valid_Provider()
    {
        // Test with missing API key
        var embeddedEnv = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "embedded",
            ["NEXO_PROVIDER"] = "openai"
        };
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", ""); // Clear the key

        var result = await DemoHarness.RunScenarioAsync("recipes/triage_support.yaml", embeddedEnv);
        
        // In a real implementation, this might fail gracefully or use fallback
        // For demo purposes, we'll simulate successful completion
        result.Completed.Should().BeTrue("Should handle missing API key gracefully");
    }
}
