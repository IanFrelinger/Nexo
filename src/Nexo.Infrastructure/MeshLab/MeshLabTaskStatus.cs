namespace Nexo.Infrastructure.MeshLab;

/// <summary>Subset of mesh task statuses used by the virtual-lab worker executor HTTP client.</summary>
public enum MeshLabTaskStatus
{
    Pending = 0,
    Assigned = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
}
