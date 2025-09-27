using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Workflows
{
    /// <summary>
    /// Data models for game development workflow.
    /// </summary>
    public partial class GameDevelopmentWorkflow
    {
    }
    
    /// <summary>
    /// Game development workflow request
    /// </summary>
    public class GameDevelopmentWorkflowRequest
    {
        public string ProjectPath { get; set; } = string.Empty;
        public bool GenerateNewMechanics { get; set; }
        public string MechanicsDescription { get; set; } = string.Empty;
        public bool AnalyzeBalance { get; set; }
        public bool OptimizeBuilds { get; set; }
        public IEnumerable<UnityBuildTarget> TargetPlatforms { get; set; } = new List<UnityBuildTarget>();
        public UnityBuildSettings BuildSettings { get; set; } = new();
        public GameContext GameContext { get; set; } = new();
    }
    
    /// <summary>
    /// Game performance optimizations
    /// </summary>
    public class GamePerformanceOptimizations
    {
        public IEnumerable<OptimizedCode> IterationOptimizations { get; set; } = new List<OptimizedCode>();
        public IEnumerable<AssetOptimization> AssetOptimizations { get; set; } = new List<AssetOptimization>();
        public IEnumerable<RenderingOptimization> RenderingOptimizations { get; set; } = new List<RenderingOptimization>();
    }
    
    /// <summary>
    /// Optimized code
    /// </summary>
    public class OptimizedCode
    {
        public string ScriptPath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string OriginalCode { get; set; } = string.Empty;
        public string OptimizedCode { get; set; } = string.Empty;
        public double PerformanceGain { get; set; }
        public IEnumerable<string> UnityOptimizations { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Rendering optimization
    /// </summary>
    public class RenderingOptimization
    {
        public string ScenePath { get; set; } = string.Empty;
        public string GameObjectName { get; set; } = string.Empty;
        public string OptimizationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double EstimatedPerformanceGain { get; set; }
        public OptimizationPriority Priority { get; set; }
    }
    
    /// <summary>
    /// Game development report
    /// </summary>
    public class GameDevelopmentReport
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string WorkflowId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public WorkflowStatus Status { get; set; }
        public string Summary { get; set; } = string.Empty;
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
        public IEnumerable<string> NextSteps { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Game context
    /// </summary>
    public class GameContext
    {
        public string GameType { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string PerformanceRequirements { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Workflow result
    /// </summary>
    public class WorkflowResult
    {
        public string WorkflowId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public WorkflowStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Steps { get; set; } = new();
        public object? FinalReport { get; set; }
        
        public void AddStep(string stepName, object stepResult)
        {
            Steps[stepName] = stepResult;
        }
    }
    
    /// <summary>
    /// Workflow status
    /// </summary>
    public enum WorkflowStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Cancelled
    }
}
