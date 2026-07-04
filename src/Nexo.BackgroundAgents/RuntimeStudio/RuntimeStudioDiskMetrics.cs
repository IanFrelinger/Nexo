using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.Objectives;

namespace Nexo.BackgroundAgents.RuntimeStudio;

public sealed record RuntimeStudioDiskMetrics(
    IReadOnlyDictionary<string, int> ObjectivesByStatus,
    ObjectiveSlaSnapshot ObjectiveSla,
    IReadOnlyDictionary<string, int> ProposalsByStatus,
    long? ObservationsFileBytes,
    int? ObservationsTailLineCount,
    DateTimeOffset? ObservationsLastTimestamp);
