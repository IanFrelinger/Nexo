using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.AI.Agents
{
    /// <summary>
    /// Gameplay analyzer interface
    /// </summary>
    public interface IGameplayAnalyzer
    {
        Task<GameplayBalanceAnalysis> AnalyzeGameplayBalanceAsync(GameplayContext context);
    }
    
    /// <summary>
    /// Balance calculator interface
    /// </summary>
    public interface IBalanceCalculator
    {
        Task<double> CalculateBalanceScoreAsync(GameplayData data);
        Task<IEnumerable<BalanceIssue>> IdentifyBalanceIssuesAsync(GameplayData data);
    }
    
    /// <summary>
    /// Gameplay balance analysis
    /// </summary>
    public partial class GameplayBalanceAnalysis
    {
        public string GameType { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public double AverageWinRate { get; set; }
        public double SkillVariance { get; set; }
        public IEnumerable<string> PopularStrategies { get; set; } = new List<string>();
        public IEnumerable<string> UnderusedStrategies { get; set; } = new List<string>();
        public IEnumerable<BalanceIssue> Issues { get; set; } = new List<BalanceIssue>();
        public double OverallBalanceScore { get; set; }
        public bool HasBalanceIssues => Issues.Any();
    }
    
    /// <summary>
    /// Balance issue
    /// </summary>
    public partial class BalanceIssue
    {
        public string Description { get; set; } = string.Empty;
        public BalanceIssueSeverity Severity { get; set; }
        public string AffectedSystem { get; set; } = string.Empty;
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Balance recommendations
    /// </summary>
    public partial class BalanceRecommendations
    {
        public string OverallStrategy { get; set; } = string.Empty;
        public IEnumerable<BalanceChange> Changes { get; set; } = new List<BalanceChange>();
    }
    
    /// <summary>
    /// Balance change
    /// </summary>
    public partial class BalanceChange
    {
        public BalanceChangeType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public BalanceChangePriority Priority { get; set; }
    }
    
    /// <summary>
    /// Balanced game mechanics
    /// </summary>
    public partial class BalancedGameMechanics
    {
        public BalanceRecommendations Recommendations { get; set; } = new();
        public ImplementationGuidance ImplementationGuidance { get; set; } = new();
        public TestingStrategy TestingStrategy { get; set; } = new();
    }
    
    /// <summary>
    /// Implementation guidance
    /// </summary>
    public partial class ImplementationGuidance
    {
        public IEnumerable<string> Steps { get; set; } = new List<string>();
        public IEnumerable<string> CodeExamples { get; set; } = new List<string>();
        public IEnumerable<string> PerformanceNotes { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Testing strategy
    /// </summary>
    public partial class TestingStrategy
    {
        public string Approach { get; set; } = string.Empty;
        public IEnumerable<string> Metrics { get; set; } = new List<string>();
        public IEnumerable<string> RollbackCriteria { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Gameplay context
    /// </summary>
    public partial class GameplayContext
    {
        public string GameType { get; set; } = string.Empty;
        public GameplayData? Data { get; set; }
        public string ProjectPath { get; set; } = string.Empty;
    }
    
    // Enums
    public enum BalanceIssueSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public enum BalanceChangeType
    {
        NumericalAdjustment,
        MechanicalChange,
        NewMechanic,
        GeneralAdjustment
    }
    
    public enum BalanceChangePriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
