namespace Nexo.Infrastructure.Maintenance.Ports;

/// <summary>
/// Optional port for pausing/resuming a blob storage service before deleting incomplete files.
/// Used by IncompleteBlobCleanupStrategy when the storage (e.g. Docker Desktop) must be stopped first.
/// Implemented by platform-specific adapters (e.g. DockerDesktopLifecycleAdapter).
/// </summary>
public interface IBlobStorageLifecycle
{
    /// <summary>Pause the service (e.g. stop Docker Desktop) so blob files can be safely deleted.</summary>
    Task PauseAsync(CancellationToken ct = default);

    /// <summary>Resume the service after cleanup.</summary>
    Task ResumeAsync(CancellationToken ct = default);
}
