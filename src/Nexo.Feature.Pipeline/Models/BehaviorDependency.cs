using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents a dependency between behaviors.
    /// </summary>
    public class BehaviorDependency
    {
        /// <summary>
        /// ID of the behavior that this behavior depends on.
        /// </summary>
        public string DependentBehaviorId { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of dependency.
        /// </summary>
        public DependencyType Type { get; set; }
        
        /// <summary>
        /// Whether the dependency is required.
        /// </summary>
        public bool IsRequired { get; set; } = true;
        
        /// <summary>
        /// Condition that must be met for the dependency to be satisfied.
        /// </summary>
        public string Condition { get; set; }
        
        /// <summary>
        /// Additional metadata about the dependency.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        public BehaviorDependency(string dependentBehaviorId, DependencyType type = DependencyType.Execution, bool isRequired = true)
        {
            DependentBehaviorId = dependentBehaviorId;
            Type = type;
            IsRequired = isRequired;
        }
        
        /// <summary>
        /// Creates a required execution dependency.
        /// </summary>
        /// <param name="dependentBehaviorId">ID of the dependent behavior.</param>
        /// <returns>A required execution dependency.</returns>
        public static BehaviorDependency Required(string dependentBehaviorId)
        {
            return new BehaviorDependency(dependentBehaviorId, DependencyType.Execution, true);
        }
        
        /// <summary>
        /// Creates an optional execution dependency.
        /// </summary>
        /// <param name="dependentBehaviorId">ID of the dependent behavior.</param>
        /// <returns>An optional execution dependency.</returns>
        public static BehaviorDependency Optional(string dependentBehaviorId)
        {
            return new BehaviorDependency(dependentBehaviorId, DependencyType.Execution, false);
        }
        
        /// <summary>
        /// Creates a data dependency.
        /// </summary>
        /// <param name="dependentBehaviorId">ID of the dependent behavior.</param>
        /// <param name="isRequired">Whether the dependency is required.</param>
        /// <returns>A data dependency.</returns>
        public static BehaviorDependency Data(string dependentBehaviorId, bool isRequired = true)
        {
            return new BehaviorDependency(dependentBehaviorId, DependencyType.Data, isRequired);
        }
        
        /// <summary>
        /// Creates a resource dependency.
        /// </summary>
        /// <param name="dependentBehaviorId">ID of the dependent behavior.</param>
        /// <param name="isRequired">Whether the dependency is required.</param>
        /// <returns>A resource dependency.</returns>
        public static BehaviorDependency Resource(string dependentBehaviorId, bool isRequired = true)
        {
            return new BehaviorDependency(dependentBehaviorId, DependencyType.Resource, isRequired);
        }
    }
} 