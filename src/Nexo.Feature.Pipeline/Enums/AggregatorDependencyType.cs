namespace Nexo.Feature.Pipeline.Enums
{
    /// <summary>
    /// Defines the types of dependencies for aggregators in the pipeline architecture.
    /// </summary>
    public enum AggregatorDependencyType
    {
        /// <summary>
        /// Execution dependency - the dependent aggregator must execute before this aggregator.
        /// </summary>
        Execution,
        
        /// <summary>
        /// Data dependency - this aggregator depends on data produced by the dependent aggregator.
        /// </summary>
        Data,
        
        /// <summary>
        /// Resource dependency - this aggregator depends on resources managed by the dependent aggregator.
        /// </summary>
        Resource,
        
        /// <summary>
        /// Conditional dependency - this aggregator depends on the dependent aggregator under certain conditions.
        /// </summary>
        Conditional,
        
        /// <summary>
        /// Soft dependency - this aggregator can execute without the dependent aggregator but may have reduced functionality.
        /// </summary>
        Soft,
        
        /// <summary>
        /// Hard dependency - this aggregator must wait for the dependent aggregator to complete successfully.
        /// </summary>
        Hard,
        
        /// <summary>
        /// Optional dependency - this aggregator can execute without the dependent aggregator.
        /// </summary>
        Optional,
        
        /// <summary>
        /// Required dependency - this aggregator cannot execute without the dependent aggregator.
        /// </summary>
        Required
    }
} 