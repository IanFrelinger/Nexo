using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Execution.Events;

/// <summary>
/// Emitted when offline mode is activated (air-gapped execution).
/// </summary>
public class OfflineModeActivatedEvent : ExecutionEvent
{
    public OfflineModeActivatedEvent()
        : base("offline_mode_activated", DateTime.UtcNow)
    {
    }
}
