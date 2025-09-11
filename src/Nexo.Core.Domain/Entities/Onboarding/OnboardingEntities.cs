using Nexo.Core.Domain.Enums.Onboarding;

namespace Nexo.Core.Domain.Entities.Onboarding
{
    /// <summary>
    /// Represents an onboarding session for a beta user
    /// </summary>
    public class OnboardingSession
    {
        public string Id { get; set; } = "";
        public string UserId { get; set; } = "";
        public OnboardingStatus Status { get; set; } = OnboardingStatus.Pending;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public OnboardingPreferences Preferences { get; set; } = new();
        public List<OnboardingStep> Steps { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// User preferences for onboarding experience
    /// </summary>
    public class OnboardingPreferences
    {
        public bool IncludeTutorial { get; set; } = true;
        public bool IncludeAdvancedFeatures { get; set; } = false;
        public string PreferredLanguage { get; set; } = "en";
        public string UserExperienceLevel { get; set; } = "beginner";
        public List<string> InterestedFeatures { get; set; } = new();
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    /// <summary>
    /// Individual step in the onboarding process
    /// </summary>
    public class OnboardingStep
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string SessionId { get; set; } = "";
        public OnboardingStepType Type { get; set; }
        public int Order { get; set; }
        public OnboardingStepStatus Status { get; set; } = OnboardingStepStatus.Pending;
        public TimeSpan EstimatedDuration { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public OnboardingStepResult? Result { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Result of executing an onboarding step
    /// </summary>
    public class OnboardingStepResult
    {
        public string StepId { get; set; } = "";
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of completing the entire onboarding process
    /// </summary>
    public class OnboardingCompletionResult
    {
        public string SessionId { get; set; } = "";
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int CompletedSteps { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Current progress of an onboarding session
    /// </summary>
    public class OnboardingProgress
    {
        public string SessionId { get; set; } = "";
        public string UserId { get; set; } = "";
        public int CompletedSteps { get; set; }
        public int TotalSteps { get; set; }
        public double ProgressPercentage { get; set; }
        public OnboardingStep? CurrentStep { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event tracked during onboarding process
    /// </summary>
    public class OnboardingEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = "";
        public OnboardingEventType EventType { get; set; }
        public string? SessionId { get; set; }
        public string? StepId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of environment validation
    /// </summary>
    public class EnvironmentValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public List<ValidationRecommendation> Recommendations { get; set; } = new();
        public Dictionary<string, object> SystemInfo { get; set; } = new();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Validation issue found during environment check
    /// </summary>
    public class ValidationIssue
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ValidationIssueType Type { get; set; }
        public string Message { get; set; } = "";
        public string? Solution { get; set; }
        public ValidationIssueSeverity Severity { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
    }

    /// <summary>
    /// Recommendation for improving the environment
    /// </summary>
    public class ValidationRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Action { get; set; }
        public ValidationRecommendationPriority Priority { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
