using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Onboarding
{
    /// <summary>
    /// Interface for tutorial services
    /// </summary>
    public interface ITutorialService
    {
        Task<string> CreateTutorialAsync(string userId, string topic);
        Task<bool> CompleteTutorialStepAsync(string userId, string tutorialId, string stepId);
        Task<List<TutorialStep>> GetTutorialStepsAsync(string tutorialId);
        Task<bool> MarkTutorialCompleteAsync(string userId, string tutorialId);
        Task<List<string>> GetAvailableTutorialsAsync();
    }

    public class TutorialStep
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
    }
}
