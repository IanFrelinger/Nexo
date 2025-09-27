using System;
using System.Collections.Generic;

namespace Nexo.Feature.Unity.Models.Unity
{
    /// <summary>
    /// Represents Unity script analysis
    /// </summary>
    public partial class UnityScriptAnalysis
    {
        public IEnumerable<UnityScript> Scripts { get; set; } = new List<UnityScript>();
        public IEnumerable<PerformanceIssue> PerformanceIssues { get; set; } = new List<PerformanceIssue>();
        public IEnumerable<CodeQualityIssue> CodeQualityIssues { get; set; } = new List<CodeQualityIssue>();
        public IEnumerable<IterationPattern> IterationPatterns { get; set; } = new List<IterationPattern>();
    }
    
    /// <summary>
    /// Represents a Unity script
    /// </summary>
    public partial class UnityScript
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public IEnumerable<UnityScriptMethod> Methods { get; set; } = new List<UnityScriptMethod>();
        public IEnumerable<UnityScriptField> Fields { get; set; } = new List<UnityScriptField>();
        public IEnumerable<UnityScriptProperty> Properties { get; set; } = new List<UnityScriptProperty>();
        public bool InheritsFromMonoBehaviour { get; set; }
        public bool InheritsFromScriptableObject { get; set; }
        public bool IsEditorScript { get; set; }
        public string ScriptType { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public int MethodCount { get; set; }
        public int FieldCount { get; set; }
        public int PropertyCount { get; set; }
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// Represents a Unity script method
    /// </summary>
    public partial class UnityScriptMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public IEnumerable<string> Parameters { get; set; } = new List<string>();
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public bool IsAbstract { get; set; }
        public int LineCount { get; set; }
        public int CyclomaticComplexity { get; set; }
        public string UnityEventType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a Unity script field
    /// </summary>
    public partial class UnityScriptField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public bool IsStatic { get; set; }
        public bool IsReadonly { get; set; }
        public bool IsConst { get; set; }
        public bool HasSerializeField { get; set; }
        public bool HasHideInInspector { get; set; }
        public bool HasTooltip { get; set; }
        public bool HasHeader { get; set; }
        public bool HasRange { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public string UnityAttributeType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a Unity script property
    /// </summary>
    public partial class UnityScriptProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public bool IsAbstract { get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
        public bool IsAutoImplemented { get; set; }
        public string GetterVisibility { get; set; } = string.Empty;
        public string SetterVisibility { get; set; } = string.Empty;
        public string UnityPropertyType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a performance issue in Unity scripts
    /// </summary>
    public partial class PerformanceIssue
    {
        public string ScriptPath { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double PerformanceImpact { get; set; }
        public string FixDifficulty { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a code quality issue in Unity scripts
    /// </summary>
    public partial class CodeQualityIssue
    {
        public string ScriptPath { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string QualityMetric { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an iteration pattern in Unity scripts
    /// </summary>
    public partial class IterationPattern
    {
        public string ScriptPath { get; set; } = string.Empty;
        public string PatternType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string IterationType { get; set; } = string.Empty;
        public string CollectionType { get; set; } = string.Empty;
        public int EstimatedIterations { get; set; }
        public string OptimizationOpportunity { get; set; } = string.Empty;
        public string PerformanceImpact { get; set; } = string.Empty;
    }
}
