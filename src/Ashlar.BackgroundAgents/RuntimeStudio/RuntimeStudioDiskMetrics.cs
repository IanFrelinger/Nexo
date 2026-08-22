using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.Objectives;

namespace Ashlar.BackgroundAgents.RuntimeStudio;

public sealed record RuntimeStudioDiskMetrics(
    IReadOnlyDictionary<string, int> ObjectivesByStatus,
    ObjectiveSlaSnapshot ObjectiveSla,
    IReadOnlyDictionary<string, int> ProposalsByStatus,
    long? ObservationsFileBytes,
    int? ObservationsTailLineCount,
    DateTimeOffset? ObservationsLastTimestamp);
