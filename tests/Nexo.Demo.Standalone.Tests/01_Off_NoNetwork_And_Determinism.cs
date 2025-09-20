using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests that Off mode has no network egress and produces deterministic results
/// </summary>
[Collection("NetworkIsolation")]
public class Off_NoNetwork_And_Determinism
{
    public Off_NoNetwork_And_Determinism()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Off_Has_NoEgress_And_Is_Deterministic()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var scenarioPath = "recipes/triage_support.yaml";
        var env = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "off"
        };

        // Act - Run the same scenario twice
        var result1 = await DemoHarness.RunScenarioAsync(scenarioPath, env);
        var result2 = await DemoHarness.RunScenarioAsync(scenarioPath, env);

        // Assert - Both runs should complete successfully
        result1.Completed.Should().BeTrue("First run should complete successfully");
        result2.Completed.Should().BeTrue("Second run should complete successfully");

        // Assert - No network calls in Off mode
        result1.NetworkAttempts.Should().Be(0, "Off mode should make no network calls");
        result2.NetworkAttempts.Should().Be(0, "Off mode should make no network calls");

        // Assert - Results should be deterministic (same hash)
        var output1Path = Path.Combine(result1.OutputDir, "outputs/labels.csv");
        var output2Path = Path.Combine(result2.OutputDir, "outputs/labels.csv");

        result1.HasOutput("outputs/labels.csv").Should().BeTrue("First run should produce labels.csv");
        result2.HasOutput("outputs/labels.csv").Should().BeTrue("Second run should produce labels.csv");

        var hash1 = DemoHarness.Sha256File(output1Path);
        var hash2 = DemoHarness.Sha256File(output2Path);

        hash1.Should().Be(hash2, "Off mode should produce identical results");

        // Assert - Compare to golden baseline
        var goldenPath = "tests/Nexo.Demo.Tests/Support/Baselines/off_labels.sha256";
        if (File.Exists(goldenPath))
        {
            var goldenHash = File.ReadAllText(goldenPath).Trim();
            hash1.Should().Be(goldenHash, "Result should match golden baseline");
        }
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Off_Mode_Produces_Consistent_Output_Format()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var scenarioPath = "recipes/triage_support.yaml";
        var env = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "off"
        };

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath, env);

        // Assert
        result.Completed.Should().BeTrue();
        result.Mode.Should().Be("off");
        result.NetworkAttempts.Should().Be(0);

        // Verify output structure
        result.HasOutput("outputs/labels.csv").Should().BeTrue();
        
        var labelsContent = result.OutputText("outputs/labels.csv");
        labelsContent.Should().Contain("id,label,confidence");
        labelsContent.Should().Contain("urgent");
        labelsContent.Should().Contain("normal");
        labelsContent.Should().Contain("low");
    }
}
