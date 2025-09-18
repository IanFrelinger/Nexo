using System.Collections.Generic;

namespace Nexo.Verify
{
    /// <summary>
    /// Represents a verification specification loaded from .verify.yaml files
    /// </summary>
    public sealed class VerifySpec
    {
        /// <summary>
        /// Path to the scenario YAML file to execute
        /// </summary>
        public string Scenario { get; init; } = "";
        
        /// <summary>
        /// Optional input file patterns (glob paths)
        /// </summary>
        public List<string>? Inputs { get; init; }
        
        /// <summary>
        /// Test modes to execute with their configurations
        /// </summary>
        public List<VerifyMode> Modes { get; init; } = new();
    }

    /// <summary>
    /// Represents a specific test mode configuration
    /// </summary>
    public sealed class VerifyMode
    {
        /// <summary>
        /// Name of the mode (e.g., "off", "hybrid_local", "embedded_cloud")
        /// </summary>
        public string Name { get; init; } = "";
        
        /// <summary>
        /// Environment variables to set for this mode
        /// </summary>
        public Dictionary<string, string>? Env { get; init; }
        
        /// <summary>
        /// Assertions to verify after execution
        /// </summary>
        public List<VerifyAssert> Asserts { get; init; } = new();
    }

    /// <summary>
    /// Represents a single assertion to verify
    /// </summary>
    public sealed class VerifyAssert
    {
        /// <summary>
        /// Type of assertion: hash_equals, no_network_egress, completes_within_ms, structure_contains, semantic_equivalence
        /// </summary>
        public string Type { get; init; } = "";
        
        /// <summary>
        /// Path to output file to hash/compare (for hash_equals, structure_contains, semantic_equivalence)
        /// </summary>
        public string? Of { get; init; }
        
        /// <summary>
        /// Expected SHA256 hash (for hash_equals)
        /// </summary>
        public string? ExpectedSha256 { get; init; }
        
        /// <summary>
        /// File path for structure checks (for structure_contains)
        /// </summary>
        public string? File { get; init; }
        
        /// <summary>
        /// Required columns for CSV structure checks (for structure_contains)
        /// </summary>
        public List<string>? MustHaveColumns { get; init; }
        
        /// <summary>
        /// Time limit in milliseconds (for completes_within_ms)
        /// </summary>
        public int? Limit { get; init; }
        
        /// <summary>
        /// Output file to compare against (for semantic_equivalence)
        /// </summary>
        public string? Against { get; init; }
        
        /// <summary>
        /// Reference/baseline file (for semantic_equivalence)
        /// </summary>
        public string? Reference { get; init; }
        
        /// <summary>
        /// Similarity metric to use (e.g., "jaccard_tokens")
        /// </summary>
        public string? Metric { get; init; }
        
        /// <summary>
        /// Minimum threshold for similarity (e.g., 0.85)
        /// </summary>
        public double? Threshold { get; init; }
    }
}
