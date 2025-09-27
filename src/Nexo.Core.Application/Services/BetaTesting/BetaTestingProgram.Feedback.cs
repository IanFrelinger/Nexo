using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.BetaTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Enums.BetaTesting;

namespace Nexo.Core.Application.Services.BetaTesting
{
    /// <summary>
    /// Feedback collection functionality
    /// </summary>
    public partial class BetaTestingProgram
    {
        /// <summary>
        /// Collects feedback from beta users
        /// </summary>
        public async Task<FeedbackCollectionResult> CollectFeedbackAsync(string programId, FeedbackCollectionRequest request)
        {
            _logger.LogInformation("Collecting feedback for program: {ProgramId}", programId);

            var collectedFeedback = new List<BetaFeedback>();
            var collectionErrors = new List<string>();

            try
            {
                // Collect in-app feedback
                if (request.IncludeInAppFeedback)
                {
                    var inAppFeedbackId = await _feedbackCollection.CollectInAppFeedbackAsync("system", programId, "in-app feedback");
                    collectedFeedback.Add(new BetaFeedback { Id = inAppFeedbackId, Type = BetaFeedbackType.GeneralFeedback, Content = "in-app feedback" });
                }

                // Collect survey feedback
                if (request.IncludeSurveyFeedback)
                {
                    var surveyFeedbackId = await _feedbackCollection.CollectSurveyFeedbackAsync("system", programId, request.SurveyId ?? "survey data");
                    collectedFeedback.Add(new BetaFeedback { Id = surveyFeedbackId, Type = BetaFeedbackType.SurveyResponse, Content = request.SurveyId ?? "survey data" });
                }

                // Collect interview feedback
                if (request.IncludeInterviewFeedback)
                {
                    var interviewFeedbackId = await _feedbackCollection.CollectInterviewFeedbackAsync("system", programId, "interview data");
                    collectedFeedback.Add(new BetaFeedback { Id = interviewFeedbackId, Type = BetaFeedbackType.InterviewResponse, Content = "interview data" });
                }

                // Process and analyze feedback
                var analysis = await AnalyzeFeedbackAsync(collectedFeedback);

                var result = new FeedbackCollectionResult
                {
                    ProgramId = programId,
                    CollectedFeedback = collectedFeedback,
                    TotalCollected = collectedFeedback.Count,
                    Analysis = analysis,
                    Errors = collectionErrors,
                    Success = !collectionErrors.Any(),
                    Timestamp = DateTime.UtcNow
                };

                // Track feedback collection
                await _analytics.TrackEventAsync("FeedbackCollected", new Dictionary<string, object>
                {
                    ["ProgramId"] = programId,
                    ["FeedbackCount"] = collectedFeedback.Count,
                    ["Analysis"] = analysis
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect feedback for program {ProgramId}", programId);
                
                return new FeedbackCollectionResult
                {
                    ProgramId = programId,
                    CollectedFeedback = new List<BetaFeedback>(),
                    TotalCollected = 0,
                    Errors = new List<string> { ex.Message },
                    Success = false,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }
}
