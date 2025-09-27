using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Feedback processing functionality for continuous learning system
    /// </summary>
    public partial class ContinuousLearningSystem
    {
        /// <summary>
        /// Processes user feedback to improve system behavior.
        /// </summary>
        public async Task ProcessUserFeedbackAsync(
            UserFeedback feedback,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing user feedback {FeedbackId} from user {UserId}", 
                feedback.FeedbackId, feedback.UserId);

            try
            {
                // Analyze feedback sentiment and content
                var feedbackAnalysis = await AnalyzeUserFeedbackAsync(feedback, cancellationToken);

                // Update user preferences based on feedback
                await UpdateUserPreferencesAsync(feedback.UserId, feedbackAnalysis, cancellationToken);

                // Generate feedback-based recommendations
                var feedbackRecommendations = await GenerateFeedbackBasedRecommendationsAsync(
                    feedback, feedbackAnalysis, cancellationToken);

                // Apply immediate improvements if possible
                foreach (var recommendation in feedbackRecommendations.Where(r => r.Priority == RecommendationPriority.Critical))
                {
                    await ApplyImmediateImprovementAsync(recommendation, cancellationToken);
                }

                // Store feedback for future learning
                await _knowledgeBase.StoreUserFeedbackAsync(feedback, feedbackAnalysis, cancellationToken);

                _logger.LogInformation("Completed processing user feedback {FeedbackId}", feedback.FeedbackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user feedback {FeedbackId}", feedback.FeedbackId);
                throw;
            }
        }

        /// <summary>
        /// Analyzes user feedback for insights.
        /// </summary>
        private async Task<Dictionary<string, object>> AnalyzeUserFeedbackAsync(
            UserFeedback feedback,
            CancellationToken cancellationToken)
        {
            var analysis = new Dictionary<string, object>
            {
                ["rating"] = feedback.Rating,
                ["type"] = feedback.Type.ToString(),
                ["sentiment"] = feedback.Rating >= 4 ? "Positive" : feedback.Rating >= 3 ? "Neutral" : "Negative",
                ["hasMessage"] = !string.IsNullOrEmpty(feedback.Message),
                ["context"] = feedback.Context
            };

            return await Task.FromResult(analysis);
        }

        /// <summary>
        /// Updates user preferences based on feedback analysis.
        /// </summary>
        private async Task UpdateUserPreferencesAsync(
            string userId,
            Dictionary<string, object> feedbackAnalysis,
            CancellationToken cancellationToken)
        {
            await _knowledgeBase.UpdateUserPreferencesAsync(userId, feedbackAnalysis, cancellationToken);
        }

        /// <summary>
        /// Generates feedback-based recommendations.
        /// </summary>
        private async Task<List<AdaptationRecommendation>> GenerateFeedbackBasedRecommendationsAsync(
            UserFeedback feedback,
            Dictionary<string, object> feedbackAnalysis,
            CancellationToken cancellationToken)
        {
            var recommendations = new List<AdaptationRecommendation>();

            if (feedback.Rating < 3)
            {
                recommendations.Add(new AdaptationRecommendation
                {
                    Type = AdaptationType.UserExperienceImprovement,
                    Title = "Address User Concerns",
                    Description = $"Address user feedback: {feedback.Message}",
                    ExpectedImprovementPercentage = 30.0,
                    ImplementationComplexity = ImplementationComplexity.Medium,
                    ConfidenceLevel = 80.0,
                    Priority = RecommendationPriority.High
                });
            }

            return await Task.FromResult(recommendations);
        }

        /// <summary>
        /// Applies immediate improvements based on critical recommendations.
        /// </summary>
        private async Task ApplyImmediateImprovementAsync(
            AdaptationRecommendation recommendation,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Applying immediate improvement: {Title}", recommendation.Title);
            await _adaptationEngine.ApplyRecommendationAsync(recommendation, cancellationToken);
        }
    }
}
