using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.BetaTesting
{
    /// <summary>
    /// Interface for beta testing program management
    /// </summary>
    public interface IBetaTestingProgram
    {
        Task<string> CreateProgramAsync(string name, string description);
        Task<bool> EnrollUserAsync(string programId, string userId);
        Task<bool> SubmitFeedbackAsync(string programId, string userId, string feedback);
        Task<List<string>> GetActiveProgramsAsync();
        Task<bool> CloseProgramAsync(string programId);
    }
}
