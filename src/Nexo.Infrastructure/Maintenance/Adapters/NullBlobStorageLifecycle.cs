using Nexo.Infrastructure.Maintenance.Ports;

namespace Nexo.Infrastructure.Maintenance.Adapters;

/// <summary>
/// No-op implementation of IBlobStorageLifecycle. Used when no pause/resume is needed
/// (e.g. Ollama blobs - GC on next run).
/// </summary>
public sealed class NullBlobStorageLifecycle : IBlobStorageLifecycle
{
    public Task PauseAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task ResumeAsync(CancellationToken ct = default) => Task.CompletedTask;
}
