using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Onboarding
{
    /// <summary>
    /// Interface for beta onboarding services
    /// </summary>
    public interface IBetaOnboardingService
    {
        Task<string> StartOnboardingAsync(string userId);
        Task<bool> CompleteStepAsync(string userId, string stepId);
        Task<List<string>> GetOnboardingStepsAsync(string userId);
        Task<bool> ValidateEnvironmentAsync(string userId);
        Task<string> GenerateTutorialAsync(string userId);
    }
}
