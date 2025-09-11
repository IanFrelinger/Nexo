using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Learning;

/// <summary>
/// Collects and processes user feedback for learning and adaptation
/// </summary>
public class UserFeedbackCollector : IUserFeedbackCollector
{
    private readonly IFeedbackStore _feedbackStore;
    private readonly IFeedbackAnalyzer _feedbackAnalyzer;
    private readonly IAdaptationEngine _adaptationEngine;
    private readonly ILogger<UserFeedbackCollector> _logger;
    
    public event EventHandler<NegativeFeedbackEventArgs>? OnNegativeFeedback;
    
    public UserFeedbackCollector(
        IFeedbackStore feedbackStore,
        IFeedbackAnalyzer feedbackAnalyzer,
        IAdaptationEngine adaptationEngine,
        ILogger<UserFeedbackCollector> logger)
    {
        _feedbackStore = feedbackStore;
        _feedbackAnalyzer = feedbackAnalyzer;
        _adaptationEngine = adaptationEngine;
        _logger = logger;
    }
    
    public async Task RecordFeedbackAsync(UserFeedback feedback)
    {
        _logger.LogInformation("Recording user feedback: {Type} - {Severity}", feedback.Type, feedback.Severity);
        
        // Store feedback with context
        await _feedbackStore.StoreFeedbackAsync(feedback);
        
        // Immediate analysis for critical feedback
        if (feedback.Severity >= FeedbackSeverity.High)
        {
            await ProcessHighSeverityFeedback(feedback);
        }
        
        // Trigger adaptation if pattern detected
        await CheckForAdaptationTriggers(feedback);
    }
    
    public async Task<IEnumerable<UserFeedback>> GetRecentFeedbackAsync(TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow - timeWindow;
        return await _feedbackStore.GetFeedbackSinceAsync(cutoffTime);
    }
    
    public async Task<FeedbackInsights> AnalyzeFeedbackTrendsAsync(TimeSpan timeWindow)
    {
        var recentFeedback = await _feedbackStore.GetFeedbackInTimeRangeAsync(
            DateTime.UtcNow - timeWindow, DateTime.UtcNow);
        
        var insights = new FeedbackInsights();
        
        // Analyze satisfaction trends
        insights.SatisfactionTrend = CalculateSatisfactionTrend(recentFeedback);
        
        // Identify common complaints
        insights.CommonComplaints = IdentifyCommonComplaints(recentFeedback);
        
        // Identify successful features
        insights.SuccessfulFeatures = IdentifySuccessfulFeatures(recentFeedback);
        
        // Platform-specific insights
        insights.PlatformInsights = AnalyzePlatformSpecificFeedback(recentFeedback);
        
        // Severity distribution
        insights.SeverityDistribution = recentFeedback.GroupBy(f => f.Severity)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Type distribution
        insights.TypeDistribution = recentFeedback.GroupBy(f => f.Type)
            .ToDictionary(g => g.Key, g => g.Count());
        
        return insights;
    }
    
    public async Task<double> CalculateOverallSatisfactionAsync(TimeSpan timeWindow)
    {
        var recentFeedback = await GetRecentFeedbackAsync(timeWindow);
        return CalculateSatisfactionScore(recentFeedback);
    }
    
    public async Task<IEnumerable<UserFeedback>> GetFeedbackByTypeAsync(FeedbackType type, TimeSpan timeWindow)
    {
        var recentFeedback = await GetRecentFeedbackAsync(timeWindow);
        return recentFeedback.Where(f => f.Type == type);
    }
    
    public async Task<IEnumerable<UserFeedback>> GetHighSeverityFeedbackAsync(TimeSpan timeWindow)
    {
        var recentFeedback = await GetRecentFeedbackAsync(timeWindow);
        return recentFeedback.Where(f => f.Severity >= FeedbackSeverity.High);
    }
    
    private async Task ProcessHighSeverityFeedback(UserFeedback feedback)
    {
        _logger.LogWarning("Processing high severity feedback: {Type} - {Severity}", feedback.Type, feedback.Severity);
        
        var analysis = await _feedbackAnalyzer.AnalyzeFeedbackAsync(feedback);
        
        if (analysis.RequiresImmediateAction)
        {
            // Trigger immediate adaptation
            await _adaptationEngine.TriggerAdaptationAsync(new AdaptationContext
            {
                Trigger = AdaptationTrigger.HighSeverityFeedback,
                Priority = AdaptationPriority.Critical,
                Context = analysis.Context.ToString(),
                UserFeedback = feedback,
                Description = $"High severity feedback: {feedback.Content}"
            });
        }
        
        // Notify listeners
        OnNegativeFeedback?.Invoke(this, new NegativeFeedbackEventArgs(feedback, new AdaptationContext()));
    }
    
    private async Task CheckForAdaptationTriggers(UserFeedback feedback)
    {
        // Check for patterns that might require adaptation
        var recentFeedback = await GetRecentFeedbackAsync(TimeSpan.FromHours(1));
        var similarFeedback = recentFeedback.Where(f => 
            f.Type == feedback.Type && 
            f.Severity >= FeedbackSeverity.Medium &&
            f.Timestamp > DateTime.UtcNow.AddHours(-1));
        
        // If we have multiple similar complaints in a short time, trigger adaptation
        if (similarFeedback.Count() >= 3)
        {
            _logger.LogWarning("Detected pattern of similar feedback, triggering adaptation");
            
            await _adaptationEngine.TriggerAdaptationAsync(new AdaptationContext
            {
                Trigger = AdaptationTrigger.UserFeedback,
                Priority = AdaptationPriority.High,
                UserFeedback = feedback,
                Description = $"Pattern of similar feedback detected: {feedback.Type}"
            });
        }
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
    
    private Dictionary<string, int> IdentifyCommonComplaints(IEnumerable<UserFeedback> feedback)
    {
        return feedback
            .Where(f => f.Severity >= FeedbackSeverity.Medium)
            .GroupBy(f => NormalizeComplaint(f.Content))
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());
    }
    
    private Dictionary<string, int> IdentifySuccessfulFeatures(IEnumerable<UserFeedback> feedback)
    {
        return feedback
            .Where(f => f.Type == FeedbackType.Feature && f.Severity <= FeedbackSeverity.Low)
            .GroupBy(f => ExtractFeatureName(f.Content))
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToDictionary(g => g.Key, g => g.Count());
    }
    
    private Dictionary<string, object> AnalyzePlatformSpecificFeedback(IEnumerable<UserFeedback> feedback)
    {
        var platformInsights = new Dictionary<string, object>();
        
        // Group feedback by platform if available in metadata
        var platformGroups = feedback
            .Where(f => f.Metadata.ContainsKey("Platform"))
            .GroupBy(f => f.Metadata["Platform"].ToString());
        
        foreach (var group in platformGroups)
        {
            var platform = group.Key ?? "Unknown";
            var satisfaction = CalculateSatisfactionScore(group);
            var complaintCount = group.Count(f => f.Severity >= FeedbackSeverity.Medium);
            
            platformInsights[platform] = new
            {
                Satisfaction = satisfaction,
                ComplaintCount = complaintCount,
                TotalFeedback = group.Count()
            };
        }
        
        return platformInsights;
    }
    
    private string NormalizeComplaint(string content)
    {
        // Simple normalization - in a real implementation, this would be more sophisticated
        var words = content.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Take(3);
        
        return string.Join(" ", words);
    }
    
    private string ExtractFeatureName(string content)
    {
        // Simple feature extraction - in a real implementation, this would be more sophisticated
        var words = content.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .Take(2);
        
        return string.Join(" ", words);
    }
    
    // IUserFeedbackCollector interface methods
    public async Task CollectFeedbackAsync(UserFeedback feedback)
    {
        await RecordFeedbackAsync(feedback);
    }
    
    public async Task<UserFeedback?> GetFeedbackAsync(string id)
    {
        return await _feedbackStore.GetFeedbackByIdAsync(id);
    }
    
    public async Task<IEnumerable<UserFeedback>> GetFeedbackAsync(DateTime startTime, DateTime endTime)
    {
        return await _feedbackStore.GetFeedbackInTimeRangeAsync(startTime, endTime);
    }
    
    public async Task<IEnumerable<UserFeedback>> GetRecentFeedbackAsync(int count = 10)
    {
        var recentFeedback = await _feedbackStore.GetFeedbackInTimeRangeAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        return recentFeedback;
    }
    
    public async Task<FeedbackAnalysisResult> AnalyzeFeedbackAsync(DateTime startTime, DateTime endTime)
    {
        var feedback = await GetFeedbackAsync(startTime, endTime);
        var analysis = await _feedbackAnalyzer.AnalyzeFeedbackBatchAsync(feedback);
        return analysis;
    }
    
    public Task DeleteOldFeedbackAsync(DateTime cutoffTime)
    {
        // TODO: Implement DeleteOldFeedbackAsync in IFeedbackStore
        // await _feedbackStore.DeleteOldFeedbackAsync(cutoffTime);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Feedback insights from analysis
/// </summary>
public class FeedbackInsights
{
    public double SatisfactionTrend { get; set; }
    public Dictionary<string, int> CommonComplaints { get; set; } = new();
    public Dictionary<string, int> SuccessfulFeatures { get; set; } = new();
    public Dictionary<string, object> PlatformInsights { get; set; } = new();
    public Dictionary<FeedbackSeverity, int> SeverityDistribution { get; set; } = new();
    public Dictionary<FeedbackType, int> TypeDistribution { get; set; } = new();
}

/// <summary>
/// Interface for feedback storage
/// </summary>
public interface IFeedbackStore
{
    Task StoreFeedbackAsync(UserFeedback feedback);
    Task<IEnumerable<UserFeedback>> GetFeedbackSinceAsync(DateTime since);
    Task<IEnumerable<UserFeedback>> GetFeedbackInTimeRangeAsync(DateTime start, DateTime end);
    Task<UserFeedback?> GetFeedbackByIdAsync(string feedbackId);
    Task DeleteFeedbackAsync(string feedbackId);
}

/// <summary>
/// Interface for feedback analysis
/// </summary>
public interface IFeedbackAnalyzer
{
    Task<FeedbackAnalysisResult> AnalyzeFeedbackAsync(UserFeedback feedback);
    Task<FeedbackAnalysisResult> AnalyzeFeedbackBatchAsync(IEnumerable<UserFeedback> feedback);
    Task<bool> RequiresImmediateActionAsync(UserFeedback feedback);
}

/// <summary>
/// Result of feedback analysis
/// </summary>
public class FeedbackAnalysisResult
{
    public bool RequiresImmediateAction { get; set; }
    public SystemState? Context { get; set; }
    public string Analysis { get; set; } = string.Empty;
    public Dictionary<string, object> Insights { get; set; } = new();
    public IEnumerable<string> RecommendedActions { get; set; } = Enumerable.Empty<string>();
}