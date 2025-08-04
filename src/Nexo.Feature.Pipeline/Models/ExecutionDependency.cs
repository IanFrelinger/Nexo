using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents a dependency between pipeline components.
    /// </summary>
    public class ExecutionDependency
    {
        /// <summary>
        /// Gets or sets the ID of the dependent component.
        /// </summary>
        public string DependentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the dependency.
        /// </summary>
        public string DependencyId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of dependency.
        /// </summary>
        public Nexo.Feature.Pipeline.Enums.DependencyType DependencyType { get; set; }

        /// <summary>
        /// Gets or sets whether this dependency is required.
        /// </summary>
        public bool IsRequired { get; set; }
    }
} 