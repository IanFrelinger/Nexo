using Nexo.VisualVerification;
using Nexo.VisualVerification.Providers;

namespace Nexo.ToyRenderer;

/// <summary>Target provider that drives the toy renderer into a simulated display.</summary>
public sealed class ToyRendererTargetAppProvider : ITargetAppProvider
{
    private readonly SimulatedDisplayProvider _display;
    private readonly ToyRenderer _renderer = new();
    private ToyScene? _scene;
    private byte[]? _framebuffer;
    private int _width;
    private int _height;

    public ToyRendererTargetAppProvider(SimulatedDisplayProvider display)
    {
        _display = display;
    }

    public Task LaunchAsync(
        BuildArtifactRef build,
        DisplayConfig display,
        ScenarioDefinition scenario,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _width = display.Width;
        _height = display.Height;
        _framebuffer = _display.Framebuffer;
        if (_framebuffer.Length == 0)
        {
            _framebuffer = new byte[checked(_width * _height * 4)];
            _display.AttachFramebuffer(_framebuffer);
        }

        _scene = new ToyScene(scenario);
        Render();
        return Task.CompletedTask;
    }

    public Task StepAsync(int step, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_scene is null)
            throw new InvalidOperationException("Toy renderer has not been launched.");

        _scene.Step(step);
        Render();
        return Task.CompletedTask;
    }

    public Task<bool> HasExitedAsync() => Task.FromResult(_scene?.HasExited ?? true);

    public ValueTask DisposeAsync()
    {
        _scene = null;
        return ValueTask.CompletedTask;
    }

    private void Render()
    {
        if (_scene is null || _framebuffer is null)
            return;

        _renderer.Render(_scene, _framebuffer, _width, _height);
    }
}
