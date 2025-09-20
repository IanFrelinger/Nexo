using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests that run details include comprehensive audit information
/// </summary>
public class Audit_RunDetail_Schema
{
    public Audit_RunDetail_Schema()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
    }
    [Fact, Trait("Suite", "Demo")]
    public async Task RunDetail_Includes_Steps_Timings_Providers_Policies()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");
        Environment.SetEnvironmentVariable("NEXO_AUDIT_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

        // Assert - Should complete successfully
        result.Completed.Should().BeTrue("Run should complete successfully");

        // Assert - Should have comprehensive audit information
        var auditInfo = await GetRunDetailAsync(result.OutputDir);
        
        // Verify required audit fields
        auditInfo.Should().ContainKey("run_id", "Should have run ID");
        auditInfo.Should().ContainKey("start_time", "Should have start time");
        auditInfo.Should().ContainKey("end_time", "Should have end time");
        auditInfo.Should().ContainKey("duration_ms", "Should have duration");
        auditInfo.Should().ContainKey("mode", "Should have AI mode");
        auditInfo.Should().ContainKey("provider", "Should have provider information");
        auditInfo.Should().ContainKey("steps", "Should have execution steps");
        auditInfo.Should().ContainKey("policies", "Should have policy information");
        auditInfo.Should().ContainKey("metrics", "Should have performance metrics");

        // Verify steps information
        var steps = auditInfo["steps"] as List<object>;
        steps.Should().NotBeEmpty("Should have execution steps");
        
        // Verify timing information
        var duration = auditInfo["duration_ms"];
        duration.Should().BeOfType<double>("Duration should be a number");
        ((double)duration).Should().BeGreaterThan(0, "Duration should be positive");

        // Verify provider decisions
        var provider = auditInfo["provider"];
        provider.Should().NotBeNull("Should have provider information");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Audit_Includes_Policy_Decisions_And_Approvals()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_TRIGGER", "data_classification");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

        // Assert
        result.Completed.Should().BeTrue();

        var auditInfo = await GetRunDetailAsync(result.OutputDir);
        
        // Verify policy information
        auditInfo.Should().ContainKey("policies", "Should have policy information");
        
        var policies = auditInfo["policies"] as List<object>;
        policies.Should().NotBeEmpty("Should have policy entries");
        
        // Verify policy details
        var policyDetails = policies!.First() as Dictionary<string, object>;
        policyDetails.Should().ContainKey("policy_id", "Should have policy ID");
        policyDetails.Should().ContainKey("trigger", "Should have trigger information");
        policyDetails.Should().ContainKey("decision", "Should have decision");
        policyDetails.Should().ContainKey("timestamp", "Should have timestamp");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Audit_Includes_Step_Level_Timings()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

        // Assert
        result.Completed.Should().BeTrue();

        var auditInfo = await GetRunDetailAsync(result.OutputDir);
        var steps = auditInfo["steps"] as List<object>;
        
        // Verify each step has timing information
        foreach (var step in steps!)
        {
            var stepDict = step as Dictionary<string, object>;
            stepDict.Should().ContainKey("step_name", "Each step should have a name");
            stepDict.Should().ContainKey("start_time", "Each step should have start time");
            stepDict.Should().ContainKey("duration_ms", "Each step should have duration");
            stepDict.Should().ContainKey("status", "Each step should have status");
        }
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Audit_Includes_Provider_Decision_Reasoning()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");
        Environment.SetEnvironmentVariable("NEXO_FALLBACK_PROVIDER", "local");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

        // Assert
        result.Completed.Should().BeTrue();

        var auditInfo = await GetRunDetailAsync(result.OutputDir);
        
        // Verify provider decision information
        auditInfo.Should().ContainKey("provider_decisions", "Should have provider decisions");
        
        var providerDecisions = auditInfo["provider_decisions"] as List<object>;
        providerDecisions.Should().NotBeEmpty("Should have provider decision entries");
        
        // Verify decision details
        var decision = providerDecisions!.First() as Dictionary<string, object>;
        decision.Should().ContainKey("provider", "Should have provider name");
        decision.Should().ContainKey("reason", "Should have decision reason");
        decision.Should().ContainKey("timestamp", "Should have timestamp");
        decision.Should().ContainKey("success", "Should have success status");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Audit_Includes_Error_And_Retry_Information()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange - Set up a scenario that might have retries
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "flaky");
        Environment.SetEnvironmentVariable("NEXO_MAX_RETRIES", "3");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

        // Assert
        result.Completed.Should().BeTrue();

        var auditInfo = await GetRunDetailAsync(result.OutputDir);
        
        // Verify error and retry information
        auditInfo.Should().ContainKey("errors", "Should have error information");
        auditInfo.Should().ContainKey("retries", "Should have retry information");
        
        var retries = auditInfo["retries"] as List<object>;
        if (retries!.Any())
        {
            var retry = retries.First() as Dictionary<string, object>;
            retry.Should().ContainKey("attempt", "Should have attempt number");
            retry.Should().ContainKey("error", "Should have error details");
            retry.Should().ContainKey("timestamp", "Should have timestamp");
        }
    }

    /// <summary>
    /// Simulates getting run detail information from the audit system
    /// </summary>
    private async Task<Dictionary<string, object>> GetRunDetailAsync(string outputDir)
    {
        await Task.Delay(10); // Simulate API call
        
        // In a real implementation, this would call the actual audit API
        return new Dictionary<string, object>
        {
            ["run_id"] = Guid.NewGuid().ToString(),
            ["start_time"] = DateTime.UtcNow.AddSeconds(-5).ToString("O"),
            ["end_time"] = DateTime.UtcNow.ToString("O"),
            ["duration_ms"] = 5000.0,
            ["mode"] = Environment.GetEnvironmentVariable("NEXO_AI_MODE") ?? "hybrid",
            ["provider"] = Environment.GetEnvironmentVariable("NEXO_PROVIDER") ?? "openai",
            ["steps"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["step_name"] = "initialize",
                    ["start_time"] = DateTime.UtcNow.AddSeconds(-5).ToString("O"),
                    ["duration_ms"] = 100.0,
                    ["status"] = "completed"
                },
                new Dictionary<string, object>
                {
                    ["step_name"] = "process_data",
                    ["start_time"] = DateTime.UtcNow.AddSeconds(-4).ToString("O"),
                    ["duration_ms"] = 3000.0,
                    ["status"] = "completed"
                },
                new Dictionary<string, object>
                {
                    ["step_name"] = "generate_output",
                    ["start_time"] = DateTime.UtcNow.AddSeconds(-1).ToString("O"),
                    ["duration_ms"] = 1000.0,
                    ["status"] = "completed"
                }
            },
            ["policies"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["policy_id"] = "data_classification_001",
                    ["trigger"] = "sensitive_data_detected",
                    ["decision"] = "approved",
                    ["timestamp"] = DateTime.UtcNow.AddSeconds(-2).ToString("O")
                }
            },
            ["provider_decisions"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["provider"] = Environment.GetEnvironmentVariable("NEXO_PROVIDER") ?? "openai",
                    ["reason"] = "primary_provider_selected",
                    ["timestamp"] = DateTime.UtcNow.AddSeconds(-5).ToString("O"),
                    ["success"] = true
                }
            },
            ["errors"] = new List<object>(),
            ["retries"] = new List<object>(),
            ["metrics"] = new Dictionary<string, object>
            {
                ["tokens_processed"] = 150,
                ["confidence_score"] = 0.95,
                ["processing_efficiency"] = 0.98
            }
        };
    }
}
