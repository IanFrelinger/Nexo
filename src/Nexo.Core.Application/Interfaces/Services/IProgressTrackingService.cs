using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Onboarding
{
    /// <summary>
    /// Interface for progress tracking services
    /// </summary>
    public interface IProgressTrackingService
    {
        Task<bool> TrackProgressAsync(string userId, string activityId, double progress);
        Task<ProgressInfo> GetProgressAsync(string userId, string activityId);
        Task<List<ProgressInfo>> GetAllProgressAsync(string userId);
        Task<bool> ResetProgressAsync(string userId, string activityId);
        Task TrackEventAsync(string eventName, Dictionary<string, object> properties);
    }

    public class ProgressInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string ActivityId { get; set; } = string.Empty;
        public double Progress { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsCompleted { get; set; }
    }
}
