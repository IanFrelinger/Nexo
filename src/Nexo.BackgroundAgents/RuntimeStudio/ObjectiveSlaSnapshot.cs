using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.Objectives;

namespace Nexo.BackgroundAgents.RuntimeStudio;

public sealed record ObjectiveSlaSnapshot(
    double? OldestPendingAgeHours,
    double? OldestInProgressAgeHours,
    int PendingCount,
    int InProgressCount,
    int BlockedCount);
