using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Models.CodeQuality
{
    /// <summary>
    /// Represents a comprehensive quality score for generated code
    /// </summary>
    public partial class QualityScore
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ToolName { get; set; } = "";
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        
        // Individual component scores (0-10)
        public double SecurityScore { get; set; }
        public double PerformanceScore { get; set; }
        public double MaintainabilityScore { get; set; }
        public double TestabilityScore { get; set; }
        public double ReadabilityScore { get; set; }
        public double DocumentationScore { get; set; }
        
        // Overall weighted score
        public double OverallScore { get; set; }
        
        // Quality level
        public QualityLevel QualityLevel { get; set; }
        
        // Detailed analysis
        public List<QualityIssue> Issues { get; set; } = new();
        public List<QualityRecommendation> Recommendations { get; set; } = new();
        
        // Metadata
        public int LinesOfCode { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int NumberOfMethods { get; set; }
        public int NumberOfClasses { get; set; }
        
        // Validation
        public bool PassesQualityGate { get; set; }
        public bool RequiresHumanReview { get; set; }
        public bool IsProductionReady { get; set; }
    }

    /// <summary>
    /// Quality levels for code assessment
    /// </summary>
    public enum QualityLevel
    {
        /// <summary>
        /// Excellent quality (9.0-10.0)
        /// </summary>
        Excellent = 5,
        
        /// <summary>
        /// Good quality (7.0-8.9)
        /// </summary>
        Good = 4,
        
        /// <summary>
        /// Acceptable quality (5.0-6.9)
        /// </summary>
        Acceptable = 3,
        
        /// <summary>
        /// Poor quality (3.0-4.9)
        /// </summary>
        Poor = 2,
        
        /// <summary>
        /// Unacceptable quality (0.0-2.9)
        /// </summary>
        Unacceptable = 1
    }


    /// <summary>
    /// Represents a quality improvement recommendation
    /// </summary>
    public partial class QualityRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public RecommendationType Type { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Implementation { get; set; } = "";
        public int Priority { get; set; }
        public double ImpactScore { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }


    /// <summary>
    /// Types of quality recommendations
    /// </summary>
    public enum RecommendationType
    {
        Security,
        Performance,
        Maintainability,
        Testability,
        Readability,
        Documentation,
        CodeStyle,
        Architecture,
        ErrorHandling,
        ResourceManagement
    }
}
