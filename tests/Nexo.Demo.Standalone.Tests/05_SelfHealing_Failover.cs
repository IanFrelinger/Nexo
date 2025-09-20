using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests self-healing capabilities including retry, backoff, and failover
/// </summary>
[Collection("NetworkIsolation")]
public class SelfHealing_Failover
{
    public SelfHealing_Failover()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
    }
    [Fact, Trait("Suite", "Demo")]
    public async Task Flaky_Primary_Fails_Once_Then_Failover_Completes()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange - Set up flaky primary and healthy secondary
        var scenarioPath = "recipes/triage_support.yaml";
        var env = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "flaky-primary",
            ["NEXO_FALLBACK_PROVIDER"] = "healthy-secondary"
        };

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath, env);

        // Assert - Should complete successfully despite initial failure
        result.Completed.Should().BeTrue("Should complete successfully after failover");
        
        // Assert - Should have attempted network calls (retry + failover)
        result.NetworkAttempts.Should().BeGreaterThan(0, "Should have made network attempts during retry/failover");
        
        // Assert - Should have output despite failures
        result.HasOutput("outputs/labels.csv").Should().BeTrue("Should produce output after failover");

        // Assert - Logs should contain retry and failover information
        var retryLogs = result.Logs.Where(log => 
            log.Contains("retry", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("failover", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("fallback", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        retryLogs.Should().NotBeEmpty("Logs should contain retry/failover information");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Circuit_Breaker_Prevents_Cascade_Failures()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange - Simulate a scenario where primary provider is completely down
        var scenarioPath = "recipes/triage_support.yaml";
        var env = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "down",
            ["NEXO_FALLBACK_PROVIDER"] = "healthy-secondary",
            ["NEXO_CIRCUIT_BREAKER_ENABLED"] = "true"
        };

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath, env);

        // Assert - Should complete using fallback
        result.Completed.Should().BeTrue("Should complete using fallback provider");
        
        // Assert - Should not make excessive retry attempts (circuit breaker)
        result.NetworkAttempts.Should().BeLessOrEqualTo(5, "Circuit breaker should limit retry attempts");
        
        // Assert - Should use fallback provider
        result.Provider.Should().Be("healthy-secondary", "Should use fallback provider");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Exponential_Backoff_Reduces_Load_On_Failing_Service()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange - Set up a provider that fails intermittently
        var scenarioPath = "recipes/triage_support.yaml";
        var env = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "intermittent-failure",
            ["NEXO_BACKOFF_ENABLED"] = "true"
        };
        var startTime = DateTime.UtcNow;

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath, env);

        // Assert - Should complete successfully
        result.Completed.Should().BeTrue("Should complete despite intermittent failures");
        
        // Assert - Should have taken some time due to backoff
        var duration = DateTime.UtcNow - startTime;
        duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100), "Backoff should add some delay");
        
        // Assert - Should have made multiple attempts
        result.NetworkAttempts.Should().BeGreaterThan(1, "Should have made multiple attempts with backoff");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Self_Healing_Recovers_From_Transient_Errors()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange - Simulate transient network issues
        var scenarioPath = "recipes/triage_support.yaml";
        var env = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "transient-errors",
            ["NEXO_MAX_RETRIES"] = "3"
        };

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath, env);

        // Assert - Should eventually succeed
        result.Completed.Should().BeTrue("Should recover from transient errors");
        
        // Assert - Should have retried
        result.NetworkAttempts.Should().BeGreaterThan(1, "Should have retried on transient errors");
        
        // Assert - Should have output
        result.HasOutput("outputs/labels.csv").Should().BeTrue("Should produce output after recovery");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Failover_Chain_Works_With_Multiple_Providers()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange - Set up a chain of providers: primary -> secondary -> tertiary
        var scenarioPath = "recipes/triage_support.yaml";
        var env = new Dictionary<string, string>
        {
            ["NEXO_AI_MODE"] = "hybrid",
            ["NEXO_PROVIDER"] = "primary-down",
            ["NEXO_FALLBACK_PROVIDER"] = "secondary-down",
            ["NEXO_TERTIARY_PROVIDER"] = "tertiary-healthy"
        };

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath, env);

        // Assert - Should complete using tertiary provider
        result.Completed.Should().BeTrue("Should complete using tertiary provider");
        
        // Assert - Should have attempted multiple providers
        result.NetworkAttempts.Should().BeGreaterThan(2, "Should have tried multiple providers");
        
        // Assert - Should use the working provider
        result.Provider.Should().Be("tertiary-healthy", "Should use the working provider in the chain");
    }
}
