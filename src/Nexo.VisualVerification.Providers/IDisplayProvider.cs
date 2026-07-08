namespace Nexo.VisualVerification.Providers;

/// <summary>Owns the virtual display lifecycle and frame capture.</summary>
public interface IDisplayProvider : IAsyncDisposable
{
    Task<DisplayConfig> StartAsync(int width, int height, CancellationToken ct);

    Task<byte[]> CaptureFrameAsync(CancellationToken ct);
}
