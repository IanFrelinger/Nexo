using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests self-healing capabilities including retry, backoff, and failover
/// </summary>
public class SelfHealing_Failover
{
    [Fact, Trait("Suite", "Demo")]
    public async Task Flaky_Primary_Fails_Once_Then_Failover_Completes()
    {
        // Arrange - Set up flaky primary and healthy secondary
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "flaky-primary");
        Environment.SetEnvironmentVariable("NEXO_FALLBACK_PROVIDER", "healthy-secondary");
        
        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

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
        // Arrange - Simulate a scenario where primary provider is completely down
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "down");
        Environment.SetEnvironmentVariable("NEXO_FALLBACK_PROVIDER", "healthy-secondary");
        Environment.SetEnvironmentVariable("NEXO_CIRCUIT_BREAKER_ENABLED", "true");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

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
        // Arrange - Set up a provider that fails intermittently
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "intermittent-failure");
        Environment.SetEnvironmentVariable("NEXO_BACKOFF_ENABLED", "true");

        var scenarioPath = "recipes/triage_support.yaml";
        var startTime = DateTime.UtcNow;

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

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
        // Arrange - Simulate transient network issues
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "transient-errors");
        Environment.SetEnvironmentVariable("NEXO_MAX_RETRIES", "3");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

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
        // Arrange - Set up a chain of providers: primary -> secondary -> tertiary
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "primary-down");
        Environment.SetEnvironmentVariable("NEXO_FALLBACK_PROVIDER", "secondary-down");
        Environment.SetEnvironmentVariable("NEXO_TERTIARY_PROVIDER", "tertiary-healthy");

        var scenarioPath = "recipes/triage_support.yaml";

        // Act
        var result = await DemoHarness.RunScenarioAsync(scenarioPath);

        // Assert - Should complete using tertiary provider
        result.Completed.Should().BeTrue("Should complete using tertiary provider");
        
        // Assert - Should have attempted multiple providers
        result.NetworkAttempts.Should().BeGreaterThan(2, "Should have tried multiple providers");
        
        // Assert - Should use the working provider
        result.Provider.Should().Be("tertiary-healthy", "Should use the working provider in the chain");
    }
}
