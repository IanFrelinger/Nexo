using Ashlar.Infrastructure.Maintenance.Ports;

namespace Ashlar.Infrastructure.Maintenance.Adapters;

/// <summary>
/// No-op implementation of IBlobStorageLifecycle. Used when no pause/resume is needed
/// (e.g. Ollama blobs - GC on next run).
/// </summary>
public sealed class NullBlobStorageLifecycle : IBlobStorageLifecycle
{
    /// <summary>Pause asynchronously.</summary>
    public Task PauseAsync(CancellationToken ct = default) => Task.CompletedTask;
    /// <summary>Resume asynchronously.</summary>
    public Task ResumeAsync(CancellationToken ct = default) => Task.CompletedTask;
}
