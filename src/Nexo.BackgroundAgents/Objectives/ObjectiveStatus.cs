using System.Text;

namespace Nexo.BackgroundAgents.Objectives;

/// <summary>
/// Lifecycle states for an <see cref="ObjectiveDocument"/>. The store maps
/// each status to a folder under <c>.nexo/runtime-studio/objectives/</c> so the
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
