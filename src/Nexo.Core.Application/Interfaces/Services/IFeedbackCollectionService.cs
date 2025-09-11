using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.BetaTesting
{
    /// <summary>
    /// Interface for feedback collection services
    /// </summary>
    public interface IFeedbackCollectionService
    {
        Task<string> SubmitFeedbackAsync(string userId, string programId, string feedback);
        Task<List<string>> GetFeedbackAsync(string programId);
        Task<bool> AnalyzeFeedbackAsync(string programId);
        Task<string> GenerateReportAsync(string programId);
        Task<string> CollectInAppFeedbackAsync(string userId, string programId, string feedback);
        Task<string> CollectSurveyFeedbackAsync(string userId, string programId, string surveyData);
        Task<string> CollectInterviewFeedbackAsync(string userId, string programId, string interviewData);
    }
}
