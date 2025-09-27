using FluentAssertions;
using Nexo.Verify.Adapters;
using Nexo.Verify.Models;
using System.Net.Http;

namespace Nexo.Verify.Tests
{
    /// <summary>
    /// Tests for self-healing behavior (retries, failover, circuit breaker)
    /// </summary>
    public partial class SelfHealingTests
    {
        [Fact]
        public async Task Hybrid_FailsOver_On_429()
        {
            // Arrange
            ProviderRegistry.SetPrimary(FakeProvider.Always429());
            ProviderRegistry.SetSecondary(FakeProvider.Returns("OK"));
            var env = new Dictionary<string, string> { { "NEXO_AI_MODE", "hybrid" } };
            
            // Act
            var result = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            
            // Assert
            result.Completed.Should().BeTrue();
            result.Logs.Should().Contain(log => log.Contains("failover"));
            result.Metrics.Should().ContainKey("retries");
            result.Metrics["retries"].Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public async Task Hybrid_Retries_On_Transient_Error()
        {
            // Arrange
            var retryProvider = new FakeProvider(() => 
            {
                // Fail first two times, succeed on third
                if (ProviderRegistry.AttemptCount < 2)
                {
                    ProviderRegistry.AttemptCount++;
                    throw new HttpRequestException("500 Internal Server Error", null, System.Net.HttpStatusCode.InternalServerError);
                }
                return Task.FromResult("Success after retry");
            });
            
            ProviderRegistry.SetPrimary(retryProvider);
            var env = new Dictionary<string, string> { { "NEXO_AI_MODE", "hybrid" } };
            
            // Act
            var result = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            
            // Assert
            result.Completed.Should().BeTrue();
            result.Metrics.Should().ContainKey("retries");
            result.Metrics["retries"].Should().Be(2);
        }

        [Fact]
        public async Task Circuit_Breaker_Opens_After_Max_Failures()
        {
            // Arrange
            ProviderRegistry.SetPrimary(FakeProvider.Always429());
            var env = new Dictionary<string, string> { { "NEXO_AI_MODE", "hybrid" } };
            
            // Act - Run multiple times to trigger circuit breaker
            for (int i = 0; i < 5; i++)
            {
                await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            }
            
            // Assert
            ProviderRegistry.CircuitBreakerOpen.Should().BeTrue();
        }
    }

    /// <summary>
    /// Test registry for managing fake providers
    /// </summary>
    public static class ProviderRegistry
    {
        private static ITextGenProvider? _primary;
        private static ITextGenProvider? _secondary;
        public static int AttemptCount { get; set; }
        public static bool CircuitBreakerOpen { get; set; }

        public static void SetPrimary(ITextGenProvider provider) => _primary = provider;
        public static void SetSecondary(ITextGenProvider provider) => _secondary = provider;
        public static void Reset() => AttemptCount = 0;
    }

    /// <summary>
    /// Interface for text generation providers
    /// </summary>
    public partial interface ITextGenProvider
    {
        Task<string> CompleteAsync(string prompt, CancellationToken ct);
    }

    /// <summary>
    /// Fake provider for testing
    /// </summary>
    public sealed class FakeProvider : ITextGenProvider
    {
        private readonly Func<Task<string>> _behavior;

        private FakeProvider(Func<Task<string>> behavior) => _behavior = behavior;

        public static FakeProvider Always429() =>
            new(() => throw new HttpRequestException("429", null, System.Net.HttpStatusCode.TooManyRequests));

        public static FakeProvider Returns(string s) => new(() => Task.FromResult(s));

        public Task<string> CompleteAsync(string prompt, CancellationToken ct) => _behavior();
    }
}
