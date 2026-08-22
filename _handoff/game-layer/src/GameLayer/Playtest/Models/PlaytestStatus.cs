namespace Ashlar.Orchestration.Playtest.Models;

/// <summary>
/// Status of a playtest session.
/// </summary>
public enum PlaytestStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
