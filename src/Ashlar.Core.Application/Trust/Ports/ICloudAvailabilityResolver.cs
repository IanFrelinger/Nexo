namespace Ashlar.Core.Application.Trust.Ports;

/// <summary>
/// Resolves whether the environment is air-gapped (cloud unavailable) at runtime.
/// Sources (priority order): env var, config file, optional network probe.
/// Used at startup and optionally before cloud calls to refresh.
/// </summary>
public interface ICloudAvailabilityResolver
{
    /// <summary>
    /// Resolves whether the environment should be treated as air-gapped.
    /// When true, cloud providers (OpenAI, Azure, etc.) should not be used.
    /// </summary>
    /// <param name="refresh">If true, re-probe sources (e.g. network check). If false, use cached value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if air-gapped (cloud unavailable); false if cloud may be used.</returns>
    Task<bool> IsAirGappedAsync(bool refresh = false, CancellationToken ct = default);
}
