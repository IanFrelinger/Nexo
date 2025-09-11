using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory.Integration
{
    /// <summary>
    /// Integration mapping configuration
    /// </summary>
    public class IntegrationMapping
    {
        /// <summary>
        /// Mapping ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Mapping name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Source field
        /// </summary>
        public string SourceField { get; set; } = string.Empty;
        
        /// <summary>
        /// Target field
        /// </summary>
        public string TargetField { get; set; } = string.Empty;
        
        /// <summary>
        /// Field type
        /// </summary>
        public string FieldType { get; set; } = string.Empty;
        
        /// <summary>
        /// Transformation rules
        /// </summary>
        public List<string> TransformationRules { get; set; } = new();
        
        /// <summary>
        /// Additional options
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = new();
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Timestamp when mapping was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Timestamp when mapping was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
