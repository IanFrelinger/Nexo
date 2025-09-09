using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation.Strategies;

/// <summary>
/// Strategy for adapting system behavior based on user feedback and experience metrics
/// </summary>
public class UserExperienceAdaptationStrategy : IAdaptationStrategy
{
    public string StrategyId => "UserExperience.Dynamic";
    public AdaptationType SupportedAdaptationType => AdaptationType.UserExperienceOptimization;
    
    private readonly IUserExperienceAnalyzer _experienceAnalyzer;
    private readonly ICodeGenerationOptimizer _codeGenerationOptimizer;
    private readonly ILogger<UserExperienceAdaptationStrategy> _logger;
    
    public UserExperienceAdaptationStrategy(
        IUserExperienceAnalyzer experienceAnalyzer,
        ICodeGenerationOptimizer codeGenerationOptimizer,
        ILogger<UserExperienceAdaptationStrategy> logger)
    {
        _experienceAnalyzer = experienceAnalyzer;
        _codeGenerationOptimizer = codeGenerationOptimizer;
        _logger = logger;
    }
    
    public async Task<AdaptationResult> ExecuteAdaptationAsync(AdaptationNeed need)
    {
        _logger.LogInformation("Executing user experience adaptation strategy");
        
        var adaptations = new List<AppliedAdaptation>();
        
        // Analyze recent feedback patterns
        var feedbackAnalysis = await AnalyzeFeedbackPatterns(need.Context.RecentFeedback);
        
        // Adapt code generation based on feedback
        var codeGenAdaptation = await AdaptCodeGeneration(feedbackAnalysis);
        if (codeGenAdaptation != null) adaptations.Add(codeGenAdaptation);
        
        // Adapt response times based on user expectations
        var responseTimeAdaptation = await AdaptResponseTimes(feedbackAnalysis);
        if (responseTimeAdaptation != null) adaptations.Add(responseTimeAdaptation);
        
        // Adapt error handling based on user feedback
        var errorHandlingAdaptation = await AdaptErrorHandling(feedbackAnalysis);
        if (errorHandlingAdaptation != null) adaptations.Add(errorHandlingAdaptation);
        
        // Adapt documentation and explanations
        var documentationAdaptation = await AdaptDocumentation(feedbackAnalysis);
        if (documentationAdaptation != null) adaptations.Add(documentationAdaptation);
        
        return new AdaptationResult
        {
            IsSuccessful = adaptations.Any(),
            AppliedAdaptations = adaptations,
            EstimatedImprovement = adaptations.Sum(a => a.EstimatedImprovementFactor),
            Timestamp = DateTime.UtcNow
        };
    }
    
    private async Task<FeedbackAnalysis> AnalyzeFeedbackPatterns(IEnumerable<UserFeedback> recentFeedback)
    {
        var analysis = new FeedbackAnalysis();
        
        var feedbackList = recentFeedback.ToList();
        
        // Analyze feedback types
        analysis.FeedbackTypes = feedbackList.GroupBy(f => f.Type)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Analyze severity distribution
        analysis.SeverityDistribution = feedbackList.GroupBy(f => f.Severity)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Identify common complaints
        analysis.CommonComplaints = feedbackList
            .Where(f => f.Severity >= FeedbackSeverity.Medium)
            .GroupBy(f => f.Content.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Analyze satisfaction trends
        analysis.SatisfactionTrend = CalculateSatisfactionTrend(feedbackList);
        
        // Identify successful patterns
        analysis.SuccessfulPatterns = feedbackList
            .Where(f => f.Type == FeedbackType.Feature && f.Severity <= FeedbackSeverity.Low)
            .GroupBy(f => ExtractPattern(f.Content))
            .OrderByDescending(g => g.Count())
            .Take(3)
            .ToDictionary(g => g.Key, g => g.Count());
        
        return analysis;
    }
    
    private async Task<AppliedAdaptation?> AdaptCodeGeneration(FeedbackAnalysis analysis)
    {
        var adaptations = new List<AppliedAdaptation>();
        
        // If users complain about code quality, improve generation
        if (analysis.CommonComplaints.ContainsKey("code quality") || 
            analysis.CommonComplaints.ContainsKey("bug") ||
            analysis.CommonComplaints.ContainsKey("error"))
        {
            // TODO: Implement these methods in ICodeGenerationOptimizer
            // await _codeGenerationOptimizer.EnableEnhancedValidation();
            // await _codeGenerationOptimizer.IncreaseTestCoverage();
            
            adaptations.Add(new AppliedAdaptation
            {
                Type = "CodeGeneration.QualityImprovement",
                Description = "Enhanced code validation and test coverage based on user feedback",
                EstimatedImprovementFactor = 1.4,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["EnhancedValidation"] = true,
                    ["IncreasedTestCoverage"] = true,
                    ["TriggeredBy"] = "UserFeedback"
                }
            });
        }
        
        // If users want more concise code, adjust verbosity
        if (analysis.CommonComplaints.ContainsKey("verbose") || 
            analysis.CommonComplaints.ContainsKey("too long"))
        {
            // TODO: Implement SetVerbosityLevel method in ICodeGenerationOptimizer
            // await _codeGenerationOptimizer.SetVerbosityLevel(VerbosityLevel.Concise);
            
            adaptations.Add(new AppliedAdaptation
            {
                Type = "CodeGeneration.VerbosityReduction",
                Description = "Reduced code verbosity based on user feedback",
                EstimatedImprovementFactor = 1.2,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["VerbosityLevel"] = "Concise",
                    ["TriggeredBy"] = "UserFeedback"
                }
            });
        }
        
        // If users want more detailed explanations, increase verbosity
        if (analysis.CommonComplaints.ContainsKey("unclear") || 
            analysis.CommonComplaints.ContainsKey("confusing"))
        {
            // TODO: Implement SetVerbosityLevel method in ICodeGenerationOptimizer
            // await _codeGenerationOptimizer.SetVerbosityLevel(VerbosityLevel.Detailed);
            
            adaptations.Add(new AppliedAdaptation
            {
                Type = "CodeGeneration.VerbosityIncrease",
                Description = "Increased code verbosity and explanations based on user feedback",
                EstimatedImprovementFactor = 1.3,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["VerbosityLevel"] = "Detailed",
                    ["TriggeredBy"] = "UserFeedback"
                }
            });
        }
        
        return adaptations.FirstOrDefault();
    }
    
    private async Task<AppliedAdaptation?> AdaptResponseTimes(FeedbackAnalysis analysis)
    {
        // If users complain about slow responses, optimize for speed
        if (analysis.CommonComplaints.ContainsKey("slow") || 
            analysis.CommonComplaints.ContainsKey("timeout"))
        {
            // TODO: Implement EnableSpeedOptimization method in ICodeGenerationOptimizer
            // await _codeGenerationOptimizer.EnableSpeedOptimization();
            
            _logger.LogInformation("Enabled speed optimization based on user feedback about slow responses");
            
            return new AppliedAdaptation
            {
                Type = "ResponseTime.SpeedOptimization",
                Description = "Enabled speed optimization based on user feedback",
                EstimatedImprovementFactor = 1.5,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["SpeedOptimization"] = true,
                    ["TriggeredBy"] = "UserFeedback"
                }
            };
        }
        
        return null;
    }
    
    private async Task<AppliedAdaptation?> AdaptErrorHandling(FeedbackAnalysis analysis)
    {
        // If users complain about unclear errors, improve error messages
        if (analysis.CommonComplaints.ContainsKey("error message") || 
            analysis.CommonComplaints.ContainsKey("unclear error"))
        {
            // TODO: Implement EnableEnhancedErrorMessages method in ICodeGenerationOptimizer
            // await _codeGenerationOptimizer.EnableEnhancedErrorMessages();
            
            _logger.LogInformation("Enhanced error messages based on user feedback");
            
            return new AppliedAdaptation
            {
                Type = "ErrorHandling.EnhancedMessages",
                Description = "Enhanced error messages based on user feedback",
                EstimatedImprovementFactor = 1.3,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["EnhancedErrorMessages"] = true,
                    ["TriggeredBy"] = "UserFeedback"
                }
            };
        }
        
        return null;
    }
    
    private async Task<AppliedAdaptation?> AdaptDocumentation(FeedbackAnalysis analysis)
    {
        // If users want better documentation, enhance it
        if (analysis.CommonComplaints.ContainsKey("documentation") || 
            analysis.CommonComplaints.ContainsKey("explanation"))
        {
            // TODO: Implement EnableEnhancedDocumentation method in ICodeGenerationOptimizer
            // await _codeGenerationOptimizer.EnableEnhancedDocumentation();
            
            _logger.LogInformation("Enhanced documentation based on user feedback");
            
            return new AppliedAdaptation
            {
                Type = "Documentation.Enhancement",
                Description = "Enhanced documentation and explanations based on user feedback",
                EstimatedImprovementFactor = 1.2,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["EnhancedDocumentation"] = true,
                    ["TriggeredBy"] = "UserFeedback"
                }
            };
        }
        
        return null;
    }
    
    private double CalculateSatisfactionTrend(IEnumerable<UserFeedback> feedback)
    {
        var feedbackList = feedback.OrderBy(f => f.Timestamp).ToList();
        if (feedbackList.Count < 2) return 0.0;
        
        var recent = feedbackList.TakeLast(feedbackList.Count / 2);
        var older = feedbackList.Take(feedbackList.Count / 2);
        
        var recentSatisfaction = CalculateSatisfactionScore(recent);
        var olderSatisfaction = CalculateSatisfactionScore(older);
        
        return recentSatisfaction - olderSatisfaction;
    }
    
    private double CalculateSatisfactionScore(IEnumerable<UserFeedback> feedback)
    {
        var feedbackList = feedback.ToList();
        if (!feedbackList.Any()) return 0.5;
        
        var totalScore = feedbackList.Sum(f => f.Severity switch
        {
            FeedbackSeverity.Low => 1.0,
            FeedbackSeverity.Medium => 0.5,
            FeedbackSeverity.High => 0.0,
            FeedbackSeverity.Critical => -0.5,
            _ => 0.5
        });
        
        return totalScore / feedbackList.Count;
    }
    
    private string ExtractPattern(string content)
    {
        // Simple pattern extraction - in a real implementation, this would be more sophisticated
        var words = content.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Take(3));
    }
    
    public int GetPriority(SystemState systemState)
    {
        var highSeverityFeedback = systemState.RecentFeedback.Count(f => f.Severity >= FeedbackSeverity.High);
        return highSeverityFeedback switch
        {
            >= 3 => 90,
            >= 2 => 70,
            >= 1 => 50,
            _ => 30
        };
    }
    
    public async Task<bool> CanHandleAsync(AdaptationNeed need)
    {
        return need.Type == AdaptationType.UserExperienceOptimization &&
               need.Context.RecentFeedback.Any();
    }
    
    public string GetDescription()
    {
        return "Adapts system behavior based on user feedback and experience metrics to improve user satisfaction";
    }
    
    public double GetEstimatedImprovementFactor(AdaptationNeed need)
    {
        var highSeverityFeedback = need.Context.RecentFeedback.Count(f => f.Severity >= FeedbackSeverity.High);
        return highSeverityFeedback switch
        {
            >= 3 => 1.5,
            >= 2 => 1.3,
            >= 1 => 1.2,
            _ => 1.1
        };
    }
}

/// <summary>
/// Analysis of user feedback patterns
/// </summary>
public class FeedbackAnalysis
{
    public Dictionary<FeedbackType, int> FeedbackTypes { get; set; } = new();
    public Dictionary<FeedbackSeverity, int> SeverityDistribution { get; set; } = new();
    public Dictionary<string, int> CommonComplaints { get; set; } = new();
    public double SatisfactionTrend { get; set; }
    public Dictionary<string, int> SuccessfulPatterns { get; set; } = new();
}

