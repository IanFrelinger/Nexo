using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// User feedback collector interface
/// </summary>
public interface IUserFeedbackCollector
{
    /// <summary>
    /// Collect user feedback
    /// </summary>
    Task CollectFeedbackAsync(UserFeedback feedback);
    
    /// <summary>
    /// Get recent feedback
    /// </summary>
    Task<IEnumerable<UserFeedback>> GetRecentFeedbackAsync(TimeSpan timeSpan);
    
    /// <summary>
    /// Get feedback by type
    /// </summary>
    Task<IEnumerable<UserFeedback>> GetFeedbackByTypeAsync(FeedbackType type);
    
    /// <summary>
    /// Event fired when negative feedback is received
    /// </summary>
    event EventHandler<NegativeFeedbackEventArgs> NegativeFeedbackReceived;
    
    /// <summary>
    /// Event fired when negative feedback is received (alias for NegativeFeedbackReceived)
    /// </summary>
    event EventHandler<NegativeFeedbackEventArgs> OnNegativeFeedback;
}