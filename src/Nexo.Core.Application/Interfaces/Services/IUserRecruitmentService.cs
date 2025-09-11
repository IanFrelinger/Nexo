using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.BetaTesting
{
    /// <summary>
    /// Interface for user recruitment services
    /// </summary>
    public interface IUserRecruitmentService
    {
        Task<string> RecruitUserAsync(string email, string programId);
        Task<bool> ValidateUserAsync(string userId);
        Task<List<string>> GetRecruitedUsersAsync(string programId);
        Task<bool> SendInvitationAsync(string userId, string programId);
        Task<List<string>> RecruitUsersForSegmentAsync(string segmentId, string programId);
    }
}
