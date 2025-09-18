using FluentAssertions;
using Nexo.Verify.Adapters;
using Nexo.Verify.Models;

namespace Nexo.Verify.Tests
{
    /// <summary>
    /// Tests for deterministic behavior in OFF mode
    /// </summary>
    public class DeterminismTests
    {
        [Fact]
        public async Task OffMode_Is_Deterministic()
        {
            // Arrange
            var env = new Dictionary<string, string> { { "NEXO_AI_MODE", "off" } };
            
            // Act
            var result1 = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            var result2 = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            
            // Assert
            result1.Completed.Should().BeTrue();
            result2.Completed.Should().BeTrue();
            
            // Compare output files byte-for-byte
            var output1 = result1.OutputBytes("outputs/labels.csv");
            var output2 = result2.OutputBytes("outputs/labels.csv");
            
            output1.Should().BeEquivalentTo(output2, "OFF mode should produce identical outputs");
        }

        [Fact]
        public async Task OffMode_Produces_Consistent_Hash()
        {
            // Arrange
            var env = new Dictionary<string, string> { { "NEXO_AI_MODE", "off" } };
            
            // Act
            var result = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            
            // Assert
            result.Completed.Should().BeTrue();
            
            var output = result.OutputBytes("outputs/labels.csv");
            var hash1 = System.Security.Cryptography.SHA256.HashData(output);
            
            // Run again and verify same hash
            var result2 = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
            var output2 = result2.OutputBytes("outputs/labels.csv");
            var hash2 = System.Security.Cryptography.SHA256.HashData(output2);
            
            hash1.Should().BeEquivalentTo(hash2, "OFF mode should produce consistent hashes");
        }
    }
}
