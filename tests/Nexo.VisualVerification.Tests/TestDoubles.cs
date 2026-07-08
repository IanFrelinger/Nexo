using Nexo.VisualVerification;
using Nexo.VisualVerification.Providers;

namespace Nexo.VisualVerification.Tests;

internal sealed class HollowCaptureDisplayProvider : IDisplayProvider
{
    private DisplayConfig _config = new(0, 0, PixelFormats.Rgba8888, "hollow");

    public Task<DisplayConfig> StartAsync(int width, int height, CancellationToken ct)
    {
        _config = new DisplayConfig(width, height, PixelFormats.Rgba8888, "hollow");
        return Task.FromResult(_config);
    }

    public Task<byte[]> CaptureFrameAsync(CancellationToken ct)
    {
        var bytes = new byte[checked(_config.Width * _config.Height * 4)];
        return Task.FromResult(bytes);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class NoOpTargetAppProvider : ITargetAppProvider
{
    public Task LaunchAsync(BuildArtifactRef build, DisplayConfig display, ScenarioDefinition scenario, CancellationToken ct) =>
        Task.CompletedTask;

    public Task StepAsync(int step, CancellationToken ct) => Task.CompletedTask;

    public Task<bool> HasExitedAsync() => Task.FromResult(false);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class NondeterministicToyRendererTargetAppProvider : ITargetAppProvider
{
    private static int _nondeterminismSalt;
    private readonly SimulatedDisplayProvider _display;
    private readonly ToyRenderer.ToyRendererTargetAppProvider _inner;

    public NondeterministicToyRendererTargetAppProvider(SimulatedDisplayProvider display)
    {
        _display = display;
        _inner = new ToyRenderer.ToyRendererTargetAppProvider(display);
    }

    public Task LaunchAsync(BuildArtifactRef build, DisplayConfig display, ScenarioDefinition scenario, CancellationToken ct) =>
        _inner.LaunchAsync(build, display, scenario, ct);

    public async Task StepAsync(int step, CancellationToken ct)
    {
        await _inner.StepAsync(step, ct).ConfigureAwait(false);
        if (step == 3)
        {
            var pixels = _display.Framebuffer;
            if (pixels.Length > 0)
                pixels[0] ^= (byte)Interlocked.Increment(ref _nondeterminismSalt);
        }
    }

    public Task<bool> HasExitedAsync() => _inner.HasExitedAsync();

    public ValueTask DisposeAsync() => _inner.DisposeAsync();
}
