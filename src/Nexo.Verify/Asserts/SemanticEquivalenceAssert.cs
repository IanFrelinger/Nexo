using Nexo.Verify.Models;
using Nexo.Verify.Similarity;

namespace Nexo.Verify.Asserts
{
    /// <summary>
    /// Assert that two outputs are semantically equivalent using similarity metrics
    /// </summary>
    public static class SemanticEquivalenceAssert
    {
        /// <summary>
        /// Verify that two files are semantically equivalent using the specified metric
        /// </summary>
        /// <param name="outputFile">Path to the output file</param>
        /// <param name="referenceFile">Path to the reference/baseline file</param>
        /// <param name="metric">Similarity metric to use (e.g., "jaccard_tokens")</param>
        /// <param name="threshold">Minimum similarity threshold (0.0 to 1.0)</param>
        /// <param name="console">Console for output</param>
        /// <returns>True if similarity meets or exceeds the threshold</returns>
        public static bool Verify(string outputFile, string referenceFile, string metric, double threshold, IConsole console)
        {
            try
            {
                if (!File.Exists(outputFile))
                {
                    console.WriteLine($"ERROR: Output file not found: {outputFile}");
                    return false;
                }

                if (!File.Exists(referenceFile))
                {
                    console.WriteLine($"ERROR: Reference file not found: {referenceFile}");
                    return false;
                }

                var outputText = File.ReadAllText(outputFile);
                var referenceText = File.ReadAllText(referenceFile);

                double similarity = metric.ToLowerInvariant() switch
                {
                    "jaccard_tokens" => Jaccard.Tokens(outputText, referenceText),
                    _ => throw new ArgumentException($"Unsupported similarity metric: {metric}")
                };

                if (similarity >= threshold)
                {
                    console.WriteLine($"✓ Semantic equivalence verified: {similarity:F3} >= {threshold:F3} ({metric})");
                    return true;
                }
                else
                {
                    console.WriteLine($"✗ Semantic equivalence failed: {similarity:F3} < {threshold:F3} ({metric})");
                    console.WriteLine($"  Output:    {Path.GetFileName(outputFile)}");
                    console.WriteLine($"  Reference: {Path.GetFileName(referenceFile)}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                console.WriteLine($"ERROR: Failed to verify semantic equivalence: {ex.Message}");
                return false;
            }
        }
    }
}
