using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using ExecutionContext = Nexo.Feature.Pipeline.Models.ExecutionContext;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// User-related functionality
    /// </summary>
    public partial class KnowledgeBase
    {
        /// <summary>
        /// Stores user feedback for future analysis.
        /// </summary>
        public async Task StoreUserFeedbackAsync(
            UserFeedback feedback,
            Dictionary<string, object> analysis,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Storing user feedback {FeedbackId} from user {UserId}", 
                feedback.FeedbackId, feedback.UserId);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "UserFeedback",
                    Description = $"User feedback: {feedback.Message}",
                    ConfidenceLevel = 75.0,
                    Data = new Dictionary<string, object>
                    {
                        { "feedbackType", feedback.Type.ToString() },
                        { "rating", feedback.Rating },
                        { "userId", feedback.UserId },
                        { "analysis", analysis }
                    },
                    Source = "UserFeedback"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing user feedback");
                throw;
            }
        }

        /// <summary>
        /// Updates user preferences based on feedback analysis.
        /// </summary>
        public Task UpdateUserPreferencesAsync(
            string userId,
            Dictionary<string, object> feedbackAnalysis,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating user preferences for user {UserId}", userId);

            try
            {
                if (!_userPreferences.ContainsKey(userId))
                {
                    _userPreferences[userId] = new Dictionary<string, object>();
                }

                // Update preferences based on feedback analysis
                if (feedbackAnalysis.ContainsKey("rating"))
                {
                    _userPreferences[userId]["lastRating"] = feedbackAnalysis["rating"];
                }

                if (feedbackAnalysis.ContainsKey("sentiment"))
                {
                    _userPreferences[userId]["preferredSentiment"] = feedbackAnalysis["sentiment"];
                }

                _userPreferences[userId]["lastUpdated"] = DateTime.UtcNow;

                _logger.LogInformation("Updated user preferences for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences for user {UserId}", userId);
                throw;
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves user preferences for a specific user.
        /// </summary>
        public async Task<Dictionary<string, object>> GetUserPreferencesAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving user preferences for user {UserId}", userId);

            try
            {
                if (_userPreferences.ContainsKey(userId))
                {
                    return await Task.FromResult(_userPreferences[userId]);
                }

                return new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user preferences for user {UserId}", userId);
                return new Dictionary<string, object>();
            }
        }
    }
}
