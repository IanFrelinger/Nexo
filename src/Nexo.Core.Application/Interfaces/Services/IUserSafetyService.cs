using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Safety
{
    /// <summary>
    /// Interface for user safety services
    /// </summary>
    public interface IUserSafetyService
    {
        Task<bool> ValidateUserActionAsync(string userId, string action);
        Task<bool> ReportSafetyIssueAsync(string userId, string issue);
        Task<List<string>> GetSafetyRecommendationsAsync(string userId);
        Task<bool> EnableSafetyModeAsync(string userId);
        Task<bool> DisableSafetyModeAsync(string userId);
    }
}
