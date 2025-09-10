namespace Nexo.Core.Domain.Enums.Onboarding
{
    /// <summary>
    /// Status of onboarding sessions
    /// </summary>
    public enum OnboardingStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        Paused
    }

    /// <summary>
    /// Types of onboarding steps
    /// </summary>
    public enum OnboardingStepType
    {
        Validation,
        Installation,
        Configuration,
        Tutorial,
        ProjectCreation,
        Testing,
        Documentation
    }

    /// <summary>
    /// Status of individual onboarding steps
    /// </summary>
    public enum OnboardingStepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Skipped
    }

    /// <summary>
    /// Types of onboarding events
    /// </summary>
    public enum OnboardingEventType
    {
        SessionStarted,
        SessionCompleted,
        SessionFailed,
        StepStarted,
        StepCompleted,
        StepFailed,
        EnvironmentValidated,
        EnvironmentValidationFailed,
        TutorialCompleted,
        ProjectCreated,
        OnboardingCompleted,
        OnboardingFailed
    }

    /// <summary>
    /// Types of validation issues
    /// </summary>
    public enum ValidationIssueType
    {
        MissingDependency,
        VersionMismatch,
        PermissionDenied,
        InsufficientResources,
        ConfigurationError,
        NetworkIssue,
        SecurityIssue
    }

    /// <summary>
    /// Severity levels for validation issues
    /// </summary>
    public enum ValidationIssueSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Priority levels for validation recommendations
    /// </summary>
    public enum ValidationRecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
