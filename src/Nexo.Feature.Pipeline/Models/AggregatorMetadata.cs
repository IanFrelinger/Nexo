using System;
using System.Collections.Generic;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Metadata about an aggregator for discovery and documentation.
    /// </summary>
    public class AggregatorMetadata
    {
        /// <summary>
        /// Unique identifier for this aggregator.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Human-readable name for this aggregator.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of what this aggregator accomplishes.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Category this aggregator belongs to.
        /// </summary>
        public AggregatorCategory Category { get; set; }
        
        /// <summary>
        /// Tags for flexible organization and discovery.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Execution strategy for the behaviors within this aggregator.
        /// </summary>
        public AggregatorExecutionStrategy ExecutionStrategy { get; set; }
        
        /// <summary>
        /// Number of behaviors in this aggregator.
        /// </summary>
        public int BehaviorCount { get; set; }
        
        /// <summary>
        /// Number of direct commands in this aggregator.
        /// </summary>
        public int DirectCommandCount { get; set; }
        
        /// <summary>
        /// Dependencies that must be satisfied before this aggregator can execute.
        /// </summary>
        public List<AggregatorDependency> Dependencies { get; set; } = new List<AggregatorDependency>();
        
        /// <summary>
        /// Resource requirements for this aggregator.
        /// </summary>
        public ResourceRequirements ResourceRequirements { get; set; } = new ResourceRequirements();
        
        /// <summary>
        /// Version of this aggregator.
        /// </summary>
        public string Version { get; set; } = "1.0.0";
        
        /// <summary>
        /// Author of this aggregator.
        /// </summary>
        public string Author { get; set; } = string.Empty;
        
        /// <summary>
        /// Assembly that contains this aggregator.
        /// </summary>
        public string AssemblyName { get; set; } = string.Empty;
        
        /// <summary>
        /// Type name of this aggregator.
        /// </summary>
        public string TypeName { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether this aggregator is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        
        /// <summary>
        /// Whether this aggregator is deprecated.
        /// </summary>
        public bool IsDeprecated { get; set; } = false;
        
        /// <summary>
        /// Deprecation message if this aggregator is deprecated.
        /// </summary>
        public string? DeprecationMessage { get; set; }
        
        /// <summary>
        /// Additional metadata about this aggregator.
        /// </summary>
        public Dictionary<string, object> AdditionalMetadata { get; set; } = new Dictionary<string, object>();
    }
} 