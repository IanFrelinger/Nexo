namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// Container/stack operations used by <see cref="PocoInstallTarget"/>.
/// Kept injectable so unit tests can fake docker/compose/smoke without a live stack.
/// </summary>
public interface IPocoDeploymentOps
{
    /// <summary>Fail fast when env/ports belong to a different compose project.</summary>
    void EnsureStackIdentity();

    /// <summary>Best-effort retag of an existing image (snapshot previous tag).</summary>
    Task RetagImageAsync(string fromTag, string toTag, CancellationToken cancellationToken);

    /// <summary>Build <c>Dockerfile.custom</c> in the Poco context.</summary>
    Task<(bool Ok, string Log)> BuildCustomImageAsync(
        string pocoContext,
        string imageTag,
        CancellationToken cancellationToken);

    /// <summary>Recreate an allowed compose service (e.g. evtx).</summary>
    Task<(bool Ok, string Log)> RecreateServiceAsync(
        string service,
        bool waitHealthy,
        CancellationToken cancellationToken);

    /// <summary>Smoke health and optional /extract.</summary>
    Task<(bool Ok, string Detail)> SmokeAsync(string? extractFile, CancellationToken cancellationToken);

    /// <summary>
    /// Smoke health and optional /extract, additionally returning captured bodies
    /// keyed by sample name so an <see cref="Nexo.Core.Domain.Bricks.Ports.IAcceptanceEvaluator"/>
    /// can judge the content rather than a bare pass/fail. Implementations that cannot
    /// capture bodies inherit the default and report none.
    /// </summary>
    async Task<(bool Ok, string Detail, IReadOnlyDictionary<string, string> Outputs)> SmokeWithOutputsAsync(
        string? extractFile,
        CancellationToken cancellationToken)
    {
        var (ok, detail) = await SmokeAsync(extractFile, cancellationToken).ConfigureAwait(false);
        return (ok, detail, new Dictionary<string, string>());
    }

    /// <summary>Upsert a compose/env variable (e.g. EVTX_IMAGE).</summary>
    void UpsertEnvVar(string key, string value);
}
