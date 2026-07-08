namespace Nexo.VisualVerification.Providers;

/// <summary>Launches and drives the target build. Knows nothing about certification.</summary>
public interface ITargetAppProvider : IAsyncDisposable
{
    Task LaunchAsync(BuildArtifactRef build, DisplayConfig display, ScenarioDefinition scenario, CancellationToken ct);

    Task StepAsync(int step, CancellationToken ct);

    Task<bool> HasExitedAsync();
}
