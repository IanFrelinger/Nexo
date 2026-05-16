namespace Nexo.Core.Application.Fleet.Models;

/// <summary>
/// Lifecycle state for a mesh-scheduled unit of work (Phase 1 director).
/// </summary>
public enum MeshTaskStatus
{
    Pending = 0,
    Assigned = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4
}
