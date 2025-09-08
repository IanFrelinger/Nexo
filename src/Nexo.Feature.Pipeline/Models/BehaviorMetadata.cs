using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Metadata about a behavior for discovery and documentation.
    /// </summary>
    public class BehaviorMetadata
    {
        /// <summary>
        /// Unique identifier for this behavior.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Human-readable name for this behavior.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of what this behavior accomplishes.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Category this behavior belongs to.
        /// </summary>
        public BehaviorCategory Category { get; set; }
        
        /// <summary>
        /// Tags for flexible organization and discovery.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Execution strategy for the commands within this behavior.
        /// </summary>
        public BehaviorExecutionStrategy ExecutionStrategy { get; set; }
        
        /// <summary>
        /// Number of commands in this behavior.
        /// </summary>
        public int CommandCount { get; set; }
        
        /// <summary>
        /// Dependencies that must be satisfied before this behavior can execute.
        /// </summary>
        public List<BehaviorDependency> Dependencies { get; set; } = new List<BehaviorDependency>();
        
        /// <summary>
        /// Whether this behavior can be executed in parallel with other behaviors.
        /// </summary>
        public bool CanExecuteInParallel { get; set; }
        
        /// <summary>
        /// Version of this behavior.
        /// </summary>
        public string Version { get; set; } = "1.0.0";
        
        /// <summary>
        /// Author of this behavior.
        /// </summary>
        public string Author { get; set; } = string.Empty;
        
        /// <summary>
        /// Assembly that contains this behavior.
        /// </summary>
        public string AssemblyName { get; set; } = string.Empty;
        
        /// <summary>
        /// Type name of this behavior.
        /// </summary>
        public string TypeName { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether this behavior is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        
        /// <summary>
        /// Whether this behavior is deprecated.
        /// </summary>
        public bool IsDeprecated { get; set; } = false;
        
        /// <summary>
        /// Deprecation message if this behavior is deprecated.
        /// </summary>
        public string? DeprecationMessage { get; set; }
        
        /// <summary>
        /// Additional metadata about this behavior.
        /// </summary>
        public Dictionary<string, object> AdditionalMetadata { get; set; } = new Dictionary<string, object>();
    }
} 