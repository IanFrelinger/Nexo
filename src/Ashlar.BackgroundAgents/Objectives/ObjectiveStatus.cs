using System.Text;

namespace Ashlar.BackgroundAgents.Objectives;

/// <summary>
/// Lifecycle states for an <see cref="ObjectiveDocument"/>. The store maps
/// each status to a folder under <c>.ashlar/runtime-studio/objectives/</c> so the
/// physical layout matches the logical state and operators can move files
/// around manually if needed.
/// </summary>
public enum ObjectiveStatus
{
    Pending,
    InProgress,
    Done,
    Blocked
}
