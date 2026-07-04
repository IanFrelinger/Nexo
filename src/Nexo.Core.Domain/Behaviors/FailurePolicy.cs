using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Behaviors;

/// <summary>
/// Policy for handling step failures.
/// </summary>
public enum FailurePolicy
{
    /// <summary>Stop the behavior immediately when a step fails.</summary>
    Abort,

    /// <summary>Log the failure and continue with subsequent steps.</summary>
    Continue,

    /// <summary>Retry the failed step up to the behavior's <see cref="Behavior.MaxRetries"/>.</summary>
    Retry,

    /// <summary>Attempt a configured fallback path when the step fails.</summary>
    Fallback
}
