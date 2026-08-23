using System.Text;
using System.Text.Json.Serialization;

namespace Ashlar.BackgroundAgents.Forge;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChangeProposalStatus
{
    Proposed,
    Approved,
    Rejected,
    Applied,
    Stale
}
