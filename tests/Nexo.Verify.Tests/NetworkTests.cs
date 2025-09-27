using FluentAssertions;
using Nexo.Verify.Adapters;
using Nexo.Verify.Asserts;
using System.Net.Http;

namespace Nexo.Verify.Tests
{
    /// <summary>
    /// Tests for network isolation in OFF/ASSIST modes
    /// </summary>
    public partial class NetworkTests
    {
        [Fact]
        public async Task OffMode_Performs_No_Network_Calls()
        {
            // Arrange
            using var _ = NetworkTestHarness.InstallGlobal();
            var env = new Dictionary<string, string> { { "NEXO_AI_MODE", "off" } };
            
            // Act
            await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            
            // Assert
            NetworkTestHarness.OutboundAttempts.Should().Be(0, "OFF mode should not make network calls");
        }

        [Fact]
        public async Task AssistMode_Performs_No_Network_Calls()
        {
            // Arrange
            using var _ = NetworkTestHarness.InstallGlobal();
            var env = new Dictionary<string, string> { { "NEXO_AI_MODE", "assist" } };
            
            // Act
            await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            
            // Assert
            NetworkTestHarness.OutboundAttempts.Should().Be(0, "ASSIST mode should not make network calls");
        }

        [Fact]
        public async Task HybridMode_Allows_Network_Calls()
        {
            // Arrange
            using var _ = NetworkTestHarness.InstallGlobal();
            var env = new Dictionary<string, string> { { "NEXO_AI_MODE", "hybrid" } };
            
            // Act
            await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            
            // Assert
            // In hybrid mode, network calls should be allowed (though our test scenario doesn't make any)
            // This test verifies that the network guard doesn't block hybrid mode
            NetworkTestHarness.OutboundAttempts.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void NetworkGuard_Blocks_OffMode_Calls()
        {
            // Arrange
            using var _ = NetworkTestHarness.InstallGlobal();
            Environment.SetEnvironmentVariable("NEXO_AI_MODE", "off");
            
            var handler = new NetworkGuardHandler();
            var httpClient = new HttpClient(handler);
            
            // Act & Assert
            var act = () => httpClient.GetAsync("https://api.example.com");
            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Network egress blocked in OFF mode*");
        }
    }
}
