using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents a dependency between aggregators.
    /// </summary>
    public class AggregatorDependency
    {
        /// <summary>
        /// ID of the aggregator that this aggregator depends on.
        /// </summary>
        public string DependentAggregatorId { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of dependency.
        /// </summary>
        public AggregatorDependencyType Type { get; set; }
        
        /// <summary>
        /// Whether the dependency is required.
        /// </summary>
        public bool IsRequired { get; set; } = true;
        
        /// <summary>
        /// Condition that must be met for the dependency to be satisfied.
        /// </summary>
        public string? Condition { get; set; }
        
        /// <summary>
        /// Additional metadata about the dependency.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        public AggregatorDependency(string dependentAggregatorId, AggregatorDependencyType type = AggregatorDependencyType.Execution, bool isRequired = true)
        {
            DependentAggregatorId = dependentAggregatorId;
            Type = type;
            IsRequired = isRequired;
        }
        
        /// <summary>
        /// Creates a required execution dependency.
        /// </summary>
        /// <param name="dependentAggregatorId">ID of the dependent aggregator.</param>
        /// <returns>A required execution dependency.</returns>
        public static AggregatorDependency Required(string dependentAggregatorId)
        {
            return new AggregatorDependency(dependentAggregatorId, AggregatorDependencyType.Execution, true);
        }
        
        /// <summary>
        /// Creates an optional execution dependency.
        /// </summary>
        /// <param name="dependentAggregatorId">ID of the dependent aggregator.</param>
        /// <returns>An optional execution dependency.</returns>
        public static AggregatorDependency Optional(string dependentAggregatorId)
        {
            return new AggregatorDependency(dependentAggregatorId, AggregatorDependencyType.Execution, false);
        }
        
        /// <summary>
        /// Creates a data dependency.
        /// </summary>
        /// <param name="dependentAggregatorId">ID of the dependent aggregator.</param>
        /// <param name="isRequired">Whether the dependency is required.</param>
        /// <returns>A data dependency.</returns>
        public static AggregatorDependency Data(string dependentAggregatorId, bool isRequired = true)
        {
            return new AggregatorDependency(dependentAggregatorId, AggregatorDependencyType.Data, isRequired);
        }
        
        /// <summary>
        /// Creates a resource dependency.
        /// </summary>
        /// <param name="dependentAggregatorId">ID of the dependent aggregator.</param>
        /// <param name="isRequired">Whether the dependency is required.</param>
        /// <returns>A resource dependency.</returns>
        public static AggregatorDependency Resource(string dependentAggregatorId, bool isRequired = true)
        {
            return new AggregatorDependency(dependentAggregatorId, AggregatorDependencyType.Resource, isRequired);
        }
    }
} 