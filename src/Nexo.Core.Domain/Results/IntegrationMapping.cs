using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Represents mapping between different system data formats
    /// </summary>
    public class IntegrationMapping
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = string.Empty;
        public string TargetSystem { get; set; } = string.Empty;
        public List<FieldMapping> FieldMappings { get; set; } = new();
        public List<TransformationRule> TransformationRules { get; set; } = new();
        public MappingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class FieldMapping
    {
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
    }

    public class TransformationRule
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourceExpression { get; set; } = string.Empty;
        public string TargetExpression { get; set; } = string.Empty;
        public TransformationType Type { get; set; }
    }

    public enum MappingStatus
    {
        Draft,
        Active,
        Inactive,
        Deprecated
    }

    public enum TransformationType
    {
        Direct,
        Calculated,
        Lookup,
        Conditional,
        Custom
    }
}
