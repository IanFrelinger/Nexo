namespace Nexo.Feature.Factory.Domain.Enums
{
    /// <summary>
    /// Represents the execution strategy for feature generation.
    /// </summary>
    public enum ExecutionStrategy
    {
        /// <summary>
        /// Generate static code files (performance-critical, platform-specific features).
        /// </summary>
        Generated,

        /// <summary>
        /// Deploy runtime WASM agents (adaptive, AI-driven behavior).
        /// </summary>
        Runtime,

        /// <summary>
        /// Combined approach: Generate base code + deploy runtime agents for dynamic behavior.
        /// </summary>
        Hybrid
    }
}
