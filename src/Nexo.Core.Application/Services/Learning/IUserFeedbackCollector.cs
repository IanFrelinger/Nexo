using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Learning;

/// <summary>
/// Interface for collecting user feedback
/// </summary>
public interface IUserFeedbackCollector
{
    /// <summary>
    /// Collect user feedback
    /// </summary>
    Task CollectFeedbackAsync(UserFeedback feedback);
    
    /// <summary>
    /// Get feedback by ID
    /// </summary>
    Task<UserFeedback?> GetFeedbackAsync(string id);
    
    /// <summary>
    /// Get feedback within time range
    /// </summary>
    Task<IEnumerable<UserFeedback>> GetFeedbackAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get recent feedback
    /// </summary>
    Task<IEnumerable<UserFeedback>> GetRecentFeedbackAsync(int count = 10);
    
    /// <summary>
    /// Analyze feedback patterns
    /// </summary>
    Task<FeedbackAnalysisResult> AnalyzeFeedbackAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Event raised when negative feedback is received
    /// </summary>
    event EventHandler<Nexo.Core.Domain.Entities.Infrastructure.NegativeFeedbackEventArgs> OnNegativeFeedback;
    
    /// <summary>
    /// Delete old feedback
    /// </summary>
    Task DeleteOldFeedbackAsync(DateTime cutoffTime);
}
