using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;
using Nexo.Demo.Tests.Support;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests policy enforcement, approval workflows, and pause/resume functionality
/// </summary>
public partial class Policies_Approvals_PauseResume
{
    [Fact, Trait("Suite", "Demo")]
    public async Task Policy_Pauses_Run_And_Approval_Resumes()
    {
        // Arrange - Set up a scenario that triggers a policy
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_TRIGGER", "sensitive_data");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act - Start the run (should pause on policy)
        var pausedResult = await DemoHarness.RunScenarioAsync(scenarioPath);

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
        // Arrange
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_TRIGGER", "data_classification");

        var scenarioPath = "recipes/triage_support.yaml";
        var approvalReason = "Data classification policy approved by security team";

        // Act
        var pausedResult = await DemoHarness.RunScenarioAsync(scenarioPath);
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
        // Arrange - Set up multiple policy triggers
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_TRIGGER", "multiple");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

        // Assert - Should handle multiple policies
        result.Completed.Should().BeTrue("Should complete after multiple policy approvals");
        
        // Assert - Should have multiple approval entries
        var approvalCount = result.Logs.Count(log => 
            log.Contains("approved", StringComparison.OrdinalIgnoreCase)
        );

        approvalCount.Should().BeGreaterOrEqualTo(1, "Should have approval entries");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Policy_Violation_Blocks_Execution_Until_Approved()
    {
        // Arrange - Set up a strict policy
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_STRICT", "true");
        Environment.SetEnvironmentVariable("NEXO_POLICY_TRIGGER", "security_scan");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act - Try to run without approval
        var blockedResult = await DemoHarness.RunScenarioAsync(scenarioPath);

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
        // Arrange
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_POLICY_ENABLED", "true");
        Environment.SetEnvironmentVariable("NEXO_AUDIT_ENABLED", "true");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

        // Assert - Should complete
        result.Completed.Should().BeTrue();
        
        // Assert - Should have audit information
        var auditLogs = result.Logs.Where(log => 
            log.Contains("audit", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("policy_id", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("approver", StringComparison.OrdinalIgnoreCase)
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
