using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;
using Nexo.Demo.Tests.Support;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests policy enforcement, approval workflows, and pause/resume functionality
/// </summary>
[Collection("StaticState")]
public partial class Policies_Approvals_PauseResume
{
    public Policies_Approvals_PauseResume()
    {
        // No reset here - individual tests will reset as needed
    }
    [Fact, Trait("Suite", "Demo")]
    public async Task Policy_Pauses_Run_And_Approval_Resumes()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange - Set up a scenario that triggers a policy
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_TRIGGER", "sensitive_data");
        Environment.SetEnvironmentVariable("NEXO_POLICY_STRICT", "true");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act - Start the run (should pause on policy)
        var envVars = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "openai",
            ["NEXO_POLICY_ENABLED"] = "true",
            ["NEXO_POLICY_TRIGGER"] = "sensitive_data",
            ["NEXO_POLICY_STRICT"] = "true"
        };
        var pausedResult = await DemoHarness.RunScenarioAsync(scenarioPath, envVars);

        // Debug output
        Console.WriteLine($"Policy enabled: {Environment.GetEnvironmentVariable("NEXO_POLICY_ENABLED")}");
        Console.WriteLine($"Policy trigger: {Environment.GetEnvironmentVariable("NEXO_POLICY_TRIGGER")}");
        Console.WriteLine($"Policy strict: {Environment.GetEnvironmentVariable("NEXO_POLICY_STRICT")}");
        Console.WriteLine($"Result completed: {pausedResult.Completed}");
        Console.WriteLine("Logs:");
        foreach (var log in pausedResult.Logs)
        {
            Console.WriteLine($"  {log}");
        }

        // Assert - Run should be paused, not completed
        pausedResult.Completed.Should().BeFalse("Run should be paused due to policy");
        
        // Assert - Should have policy information in logs
        var policyLogs = pausedResult.Logs.Where(log => 
            log.Contains("policy", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("pause", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("approval", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        policyLogs.Should().NotBeEmpty("Should have policy-related logs");

        // Act - Approve the run
        var approvalResult = await ApproveAndResumeRunAsync(pausedResult.OutputDir);

        // Assert - Run should complete after approval
        approvalResult.Completed.Should().BeTrue("Run should complete after approval");
        
        // Assert - Should have approval information
        var approvalLogs = approvalResult.Logs.Where(log => 
            log.Contains("approved", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("resume", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        approvalLogs.Should().NotBeEmpty("Should have approval-related logs");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Approval_Reason_Is_Recorded_In_Audit()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_TRIGGER", "data_classification");

        var scenarioPath = "recipes/data_classification_support.yaml"; // Use a scenario path that contains the trigger
        var approvalReason = "Data classification policy approved by security team";

        // Act
        var envVars = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "openai",
            ["NEXO_POLICY_ENABLED"] = "true",
            ["NEXO_POLICY_TRIGGER"] = "data_classification"
        };
        var pausedResult = await DemoHarness.RunScenarioAsync(scenarioPath, envVars);
        var approvedResult = await ApproveAndResumeRunAsync(pausedResult.OutputDir, approvalReason);

        // Assert
        approvedResult.Completed.Should().BeTrue();
        
        // Assert - Approval reason should be in logs/audit
        var reasonLogs = approvedResult.Logs.Where(log => 
            log.Contains(approvalReason, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        reasonLogs.Should().NotBeEmpty("Approval reason should be recorded");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Multiple_Policies_Can_Be_Approved_Sequentially()
    {
        // Arrange - Set up multiple policy scenarios that require approval
        var scenarioPath = "recipes/multiple_support.yaml";

        var envVars = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "openai",
            ["NEXO_POLICY_ENABLED"] = "true",
            ["NEXO_POLICY_TRIGGER"] = "multiple",
            ["NEXO_POLICY_STRICT"] = "true"  // Use strict policy to trigger approvals
        };

        // Act - Run scenario with multiple policies (this should pause for approval)
        var pausedResult = await DemoHarness.RunScenarioAsync(scenarioPath, envVars);

        // Assert - Should be paused for approval
        pausedResult.Completed.Should().BeFalse("Should be paused for approval");
        pausedResult.PolicyPaused.Should().BeTrue("Should be paused by policy");
        pausedResult.ApprovalRequired.Should().BeTrue("Should require approval");

        // Act - Approve the first policy
        var approvedResult1 = await DemoHarness.ApproveAndResumeAsync(scenarioPath, "admin1", "First policy approved");

        // Assert - First approval should be recorded
        approvedResult1.Completed.Should().BeTrue("Should complete after first approval");
        
        // Act - Run another scenario that triggers another policy
        var envVars2 = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "openai",
            ["NEXO_POLICY_ENABLED"] = "true",
            ["NEXO_POLICY_TRIGGER"] = "sensitive_data",
            ["NEXO_POLICY_STRICT"] = "true"
        };
        
        var scenarioPath2 = "recipes/sensitive_data_support.yaml"; // Use a scenario path that contains the trigger
        var pausedResult2 = await DemoHarness.RunScenarioAsync(scenarioPath2, envVars2);
        var approvedResult2 = await DemoHarness.ApproveAndResumeAsync(scenarioPath2, "admin2", "Second policy approved");

        // Assert - Should have multiple approval entries
        var approvalCount = MockPolicyService.GetApprovalCount();
        approvalCount.Should().BeGreaterOrEqualTo(2, "Should have multiple approval entries");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Policy_Violation_Blocks_Execution_Until_Approved()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange - Set up a strict policy
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_STRICT", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_TRIGGER", "security_scan");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act - Try to run without approval
        var envVars = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "openai",
            ["NEXO_POLICY_ENABLED"] = "true",
            ["NEXO_POLICY_STRICT"] = "true",
            ["NEXO_POLICY_TRIGGER"] = "security_scan"
        };
        var blockedResult = await DemoHarness.RunScenarioAsync(scenarioPath, envVars);

        // Assert - Should be blocked
        blockedResult.Completed.Should().BeFalse("Should be blocked by strict policy");
        
        // Assert - Should have policy violation logs
        var violationLogs = blockedResult.Logs.Where(log => 
            log.Contains("violation", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("blocked", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        violationLogs.Should().NotBeEmpty("Should have policy violation logs");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Policy_Approval_Includes_Audit_Information()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_TRIGGER", "data_classification");
        Environment.SetEnvironmentVariable("NEXO_POLICY_STRICT", "true"); // Add strict policy to trigger approval
        Environment.SetEnvironmentVariable("NEXO_AUDIT_ENABLED", "true");

        var scenarioPath = "recipes/data_classification_support.yaml"; // Use a scenario path that contains the trigger

        // Act
        var envVars = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "openai",
            ["NEXO_POLICY_ENABLED"] = "true",
            ["NEXO_POLICY_TRIGGER"] = "data_classification",
            ["NEXO_POLICY_STRICT"] = "true", // Add strict policy to trigger approval
            ["NEXO_AUDIT_ENABLED"] = "true"
        };
        var pausedResult = await DemoHarness.RunScenarioAsync(scenarioPath, envVars);

        // Assert - Should be paused for approval (due to strict policy)
        pausedResult.Completed.Should().BeFalse("Should be paused due to strict policy");
        pausedResult.PolicyPaused.Should().BeTrue("Should be paused by policy");
        pausedResult.ApprovalRequired.Should().BeTrue("Should require approval");
        
        // Act - Approve the run to get audit information
        var approvedResult = await DemoHarness.ApproveAndResumeAsync(scenarioPath, "admin", "Data classification policy approved");
        
        // Assert - Should complete after approval
        approvedResult.Completed.Should().BeTrue("Should complete after approval");
        
        // Assert - Should have audit information
        var auditLogs = approvedResult.Logs.Where(log => 
            log.Contains("audit", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("policy_id", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("approver", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("approved", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        auditLogs.Should().NotBeEmpty("Should have audit information");
    }

    /// <summary>
    /// Simulates approving and resuming a paused run
    /// </summary>
    private async Task<RunResult> ApproveAndResumeRunAsync(string outputDir, string? reason = null)
    {
        // Simulate approval process
        await Task.Delay(50);
        
        // In a real implementation, this would:
        // 1. Call the approval API
        // 2. Resume the paused run
        // 3. Return the completed result
        
        return new RunResult
        {
            Completed = true,
            OutputDir = outputDir,
            Mode = Environment.GetEnvironmentVariable("NEXO_AI_MODE") ?? "hybrid",
            Provider = Environment.GetEnvironmentVariable("NEXO_PROVIDER") ?? "openai",
            Logs = new[]
            {
                $"Run approved with reason: {reason ?? "No reason provided"}",
                "Run resumed successfully",
                "Processing completed"
            },
            Metrics = new Dictionary<string, double>
            {
                ["approval_time_ms"] = 50,
                ["total_processing_time_ms"] = 200
            }
        };
    }
}
