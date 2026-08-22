using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.Objectives;

namespace Ashlar.BackgroundAgents.RuntimeStudio;

public sealed record ObjectiveSlaSnapshot(
    double? OldestPendingAgeHours,
    double? OldestInProgressAgeHours,
    int PendingCount,
    int InProgressCount,
    int BlockedCount);
