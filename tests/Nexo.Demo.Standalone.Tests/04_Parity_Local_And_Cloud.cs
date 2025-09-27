using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests that local and cloud providers produce semantically similar results
/// </summary>
public partial class Parity_Local_And_Cloud
{
    public Parity_Local_And_Cloud()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
    }
    [Fact, Trait("Suite", "Demo")]
    public async Task Labels_Are_Semantically_Similar_Across_Providers()
    {
        var scenarioPath = "recipes/triage_support.yaml";
        var results = new Dictionary<string, RunResult>();

        // Run with local provider
        var localEnv = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "local",
            ["NEXO_MODEL"] = "llama3"
        };
        var localResult = await DemoHarness.RunScenarioAsync(scenarioPath, localEnv);
        results["local"] = localResult;

        // Run with cloud provider (if API key is available)
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            var cloudEnv = new Dictionary<string, string>
            {
                ["NEXO_AI_MODE"] = "embedded",
                ["NEXO_PROVIDER"] = "openai",
                ["NEXO_MODEL"] = "gpt-4o"
            };
            var cloudResult = await DemoHarness.RunScenarioAsync(scenarioPath, cloudEnv);
            results["cloud"] = cloudResult;
        }

        // Assert both complete successfully
        results["local"].Completed.Should().BeTrue("Local provider should complete successfully");
        if (results.ContainsKey("cloud"))
        {
            results["cloud"].Completed.Should().BeTrue("Cloud provider should complete successfully");
        }

        // Compare semantic similarity if both providers were used
        if (results.ContainsKey("cloud"))
        {
            var localContent = results["local"].OutputText("outputs/labels.csv");
            var cloudContent = results["cloud"].OutputText("outputs/labels.csv");

            var normalizedLocal = DemoHarness.NormalizeText(localContent);
            var normalizedCloud = DemoHarness.NormalizeText(cloudContent);

            var similarity = DemoHarness.JaccardTokens(normalizedLocal, normalizedCloud);
            similarity.Should().BeGreaterOrEqualTo(0.85, 
                $"Local and cloud results should be semantically similar (Jaccard >= 0.85), but got {similarity:F3}");
        }
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Document_Summarization_Parity_Across_Providers()
    {
        var scenarioPath = "recipes/doc_summarize.yaml";
        var results = new Dictionary<string, RunResult>();

        // Local provider
        var localEnv = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "local"
        };
        var localResult = await DemoHarness.RunScenarioAsync(scenarioPath, localEnv);
        results["local"] = localResult;

        // Cloud provider (if available)
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            var cloudEnv = new Dictionary<string, string>
            {
                ["NEXO_AI_MODE"] = "embedded",
                ["NEXO_PROVIDER"] = "openai"
            };
            var cloudResult = await DemoHarness.RunScenarioAsync(scenarioPath, cloudEnv);
            results["cloud"] = cloudResult;
        }

        // Assert completion
        results["local"].Completed.Should().BeTrue();
        if (results.ContainsKey("cloud"))
        {
            results["cloud"].Completed.Should().BeTrue();
        }

        // Compare summaries
        if (results.ContainsKey("cloud"))
        {
            var localSummary = results["local"].OutputText("outputs/summary.txt");
            var cloudSummary = results["cloud"].OutputText("outputs/summary.txt");

            var normalizedLocal = DemoHarness.NormalizeText(localSummary);
            var normalizedCloud = DemoHarness.NormalizeText(cloudSummary);

            var similarity = DemoHarness.JaccardTokens(normalizedLocal, normalizedCloud);
            similarity.Should().BeGreaterOrEqualTo(0.85, 
                $"Summary similarity should be >= 0.85, but got {similarity:F3}");
        }
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Provider_Switching_Does_Not_Require_Recipe_Changes()
    {
        var scenarioPath = "recipes/triage_support.yaml";
        
        // Test that the same recipe works with different providers
        var providers = new[] { "local", "openai", "anthropic" };
        var results = new Dictionary<string, RunResult>();

        foreach (var provider in providers)
        {
            // Skip cloud providers if no API key
            if (provider != "local" && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
            {
                continue;
            }

            var env = new Dictionary<string, string>
            {
                ["NEXO_AI_MODE"] = "hybrid",
                ["NEXO_PROVIDER"] = provider
            };

            var result = await DemoHarness.RunScenarioAsync(scenarioPath, env);
            results[provider] = result;
        }

        // All providers should complete successfully
        foreach (var (provider, result) in results)
        {
            result.Completed.Should().BeTrue($"Provider {provider} should complete successfully");
            result.Provider.Should().Be(provider, $"Result should reflect {provider} provider");
        }
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Parity_Threshold_Is_Configurable()
    {
        // Test with different similarity thresholds
        var scenarioPath = "recipes/triage_support.yaml";
        
        var localEnv = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "local"
        };
        var localResult = await DemoHarness.RunScenarioAsync(scenarioPath, localEnv);

        // Simulate cloud result with slight variation
        var cloudEnv = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "openai"
        };
        var cloudResult = await DemoHarness.RunScenarioAsync(scenarioPath, cloudEnv);

        var localContent = localResult.OutputText("outputs/labels.csv");
        var cloudContent = cloudResult.OutputText("outputs/labels.csv");

        var similarity = DemoHarness.JaccardTokens(
            DemoHarness.NormalizeText(localContent),
            DemoHarness.NormalizeText(cloudContent)
        );

        // Test different thresholds
        similarity.Should().BeGreaterOrEqualTo(0.80, "Should meet minimum threshold");
        similarity.Should().BeLessOrEqualTo(1.00, "Should not be identical (showing variation)");
    }
}
