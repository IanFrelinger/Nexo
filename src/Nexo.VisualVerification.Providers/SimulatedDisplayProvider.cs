namespace Nexo.VisualVerification.Providers;

/// <summary>In-process framebuffer for headless CI visual verification.</summary>
public sealed class SimulatedDisplayProvider : IDisplayProvider
{
    private readonly object _sync = new();
    private byte[] _framebuffer = Array.Empty<byte>();
    private DisplayConfig _config = new(0, 0, PixelFormats.Rgba8888, "simulated");

    public byte[] Framebuffer
    {
        get
        {
            lock (_sync)
                return _framebuffer;
        }
    }

    public Span<byte> GetFramebufferSpan()
    {
        lock (_sync)
            return _framebuffer;
    }

    public void AttachFramebuffer(byte[] framebuffer)
    {
        ArgumentNullException.ThrowIfNull(framebuffer);
        lock (_sync)
            _framebuffer = framebuffer;
    }

    public Task<DisplayConfig> StartAsync(int width, int height, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var bytes = new byte[checked(width * height * 4)];
        lock (_sync)
        {
            _framebuffer = bytes;
            _config = new DisplayConfig(width, height, PixelFormats.Rgba8888, "simulated");
        }

        return Task.FromResult(_config);
    }

    public Task<byte[]> CaptureFrameAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_sync)
            return Task.FromResult(_framebuffer.ToArray());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
