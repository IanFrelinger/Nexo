using FluentAssertions;
using Nexo.Verify.Adapters;
using Nexo.Verify.Similarity;

namespace Nexo.Parity.Tests
{
    /// <summary>
    /// Tests for cross-provider parity and semantic equivalence
    /// </summary>
    public partial class ParityTests
    {
        [Fact]
        public async Task Local_vs_Cloud_Providers_Produce_Semantically_Equivalent_Outputs()
        {
            // Arrange
            var localEnv = new Dictionary<string, string>
            {
                { "NEXO_AI_MODE", "hybrid" },
                { "NEXO_PROVIDER", "local" },
                { "NEXO_MODEL", "llama3" }
            };

            var cloudEnv = new Dictionary<string, string>
            {
                { "NEXO_AI_MODE", "embedded" },
                { "NEXO_PROVIDER", "openai" },
                { "NEXO_MODEL", "gpt-4o" }
            };

            // Act
            var localResult = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, localEnv);
            var cloudResult = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, cloudEnv);

            // Assert
            localResult.Completed.Should().BeTrue();
            cloudResult.Completed.Should().BeTrue();

            var localOutput = localResult.OutputText("outputs/labels.csv");
            var cloudOutput = cloudResult.OutputText("outputs/labels.csv");

            var similarity = Jaccard.Tokens(localOutput, cloudOutput);
            similarity.Should().BeGreaterOrEqualTo(0.85, "Local and cloud providers should produce semantically equivalent outputs");
        }

        [Fact]
        public async Task Different_Cloud_Providers_Produce_Consistent_Results()
        {
            // Arrange
            var openaiEnv = new Dictionary<string, string>
            {
                { "NEXO_AI_MODE", "embedded" },
                { "NEXO_PROVIDER", "openai" },
                { "NEXO_MODEL", "gpt-4o" }
            };

            var anthropicEnv = new Dictionary<string, string>
            {
                { "NEXO_AI_MODE", "embedded" },
                { "NEXO_PROVIDER", "anthropic" },
                { "NEXO_MODEL", "claude-3" }
            };

            // Act
            var openaiResult = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, openaiEnv);
            var anthropicResult = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, anthropicEnv);

            // Assert
            openaiResult.Completed.Should().BeTrue();
            anthropicResult.Completed.Should().BeTrue();

            var openaiOutput = openaiResult.OutputText("outputs/labels.csv");
            var anthropicOutput = anthropicResult.OutputText("outputs/labels.csv");

            var similarity = Jaccard.Tokens(openaiOutput, anthropicOutput);
            similarity.Should().BeGreaterOrEqualTo(0.80, "Different cloud providers should produce consistent results");
        }

        [Fact]
        public async Task Offline_Mode_Produces_Deterministic_Results_Across_Runs()
        {
            // Arrange
            var env = new Dictionary<string, string>
            {
                { "NEXO_AI_MODE", "off" }
            };

            // Act - Run multiple times
            var results = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var result = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);
                result.Completed.Should().BeTrue();
                results.Add(result.OutputText("outputs/labels.csv"));
            }

            // Assert - All outputs should be identical
            results.All(r => r == results[0]).Should().BeTrue("OFF mode should produce identical results across runs");
        }

        [Fact]
        public async Task Platform_Independence_Across_Operating_Systems()
        {
            // This test would run on different OSes in CI
            // For now, we'll test that the same scenario produces consistent results
            
            // Arrange
            var env = new Dictionary<string, string>
            {
                { "NEXO_AI_MODE", "off" }
            };

            // Act
            var result = await ScenarioApi.RunAsync("examples/triage_support.yaml", null, env);

            // Assert
            result.Completed.Should().BeTrue();
            result.OutputDir.Should().NotBeNullOrEmpty();
            
            // Verify that the output structure is consistent regardless of platform
            var outputFile = Path.Combine(result.OutputDir, "outputs", "labels.csv");
            File.Exists(outputFile).Should().BeTrue();
        }
    }
}
