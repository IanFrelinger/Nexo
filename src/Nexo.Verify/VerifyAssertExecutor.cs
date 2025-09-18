using Nexo.Verify.Models;
using Nexo.Verify.Asserts;

namespace Nexo.Verify
{
    /// <summary>
    /// Dispatches assertion execution based on assertion type
    /// </summary>
    public static class VerifyAssertExecutor
    {
        /// <summary>
        /// Execute a single assertion
        /// </summary>
        /// <param name="assert">Assertion to execute</param>
        /// <param name="result">Scenario execution result</param>
        /// <param name="elapsedMs">Elapsed execution time in milliseconds</param>
        /// <param name="console">Console for output</param>
        /// <returns>True if assertion passed</returns>
        public static bool Execute(VerifyAssert assert, ScenarioResult result, int elapsedMs, IConsole console)
        {
            try
            {
                return assert.Type.ToLowerInvariant() switch
                {
                    "hash_equals" => ExecuteHashEquals(assert, result, console),
                    "no_network_egress" => ExecuteNoNetworkEgress(assert, result, console),
                    "completes_within_ms" => ExecuteCompletesWithin(assert, elapsedMs, console),
                    "structure_contains" => ExecuteStructureContains(assert, result, console),
                    "semantic_equivalence" => ExecuteSemanticEquivalence(assert, result, console),
                    _ => throw new ArgumentException($"Unknown assertion type: {assert.Type}")
                };
            }
            catch (Exception ex)
            {
                console.WriteLine($"ERROR: Failed to execute assertion {assert.Type}: {ex.Message}");
                return false;
            }
        }

        private static bool ExecuteHashEquals(VerifyAssert assert, ScenarioResult result, IConsole console)
        {
            if (string.IsNullOrEmpty(assert.Of) || string.IsNullOrEmpty(assert.ExpectedSha256))
            {
                console.WriteLine("ERROR: hash_equals assertion requires 'of' and 'expected_sha256' properties");
                return false;
            }

            var filePath = Path.Combine(result.OutputDir, assert.Of);
            return HashEqualsAssert.Verify(filePath, assert.ExpectedSha256, console);
        }

        private static bool ExecuteNoNetworkEgress(VerifyAssert assert, ScenarioResult result, IConsole console)
        {
            return NoNetworkEgressAssert.Verify(result, console);
        }

        private static bool ExecuteCompletesWithin(VerifyAssert assert, int elapsedMs, IConsole console)
        {
            if (!assert.Limit.HasValue)
            {
                console.WriteLine("ERROR: completes_within_ms assertion requires 'limit' property");
                return false;
            }

            return CompletesWithinAssert.Verify(elapsedMs, assert.Limit.Value, console);
        }

        private static bool ExecuteStructureContains(VerifyAssert assert, ScenarioResult result, IConsole console)
        {
            if (string.IsNullOrEmpty(assert.File) || assert.MustHaveColumns == null || !assert.MustHaveColumns.Any())
            {
                console.WriteLine("ERROR: structure_contains assertion requires 'file' and 'must_have_columns' properties");
                return false;
            }

            var filePath = Path.Combine(result.OutputDir, assert.File);
            return StructureContainsAssert.Verify(filePath, assert.MustHaveColumns, console);
        }

        private static bool ExecuteSemanticEquivalence(VerifyAssert assert, ScenarioResult result, IConsole console)
        {
            if (string.IsNullOrEmpty(assert.Against) || string.IsNullOrEmpty(assert.Reference) || 
                string.IsNullOrEmpty(assert.Metric) || !assert.Threshold.HasValue)
            {
                console.WriteLine("ERROR: semantic_equivalence assertion requires 'against', 'reference', 'metric', and 'threshold' properties");
                return false;
            }

            var outputFile = Path.Combine(result.OutputDir, assert.Against);
            return SemanticEquivalenceAssert.Verify(outputFile, assert.Reference, assert.Metric, assert.Threshold.Value, console);
        }
    }
}
