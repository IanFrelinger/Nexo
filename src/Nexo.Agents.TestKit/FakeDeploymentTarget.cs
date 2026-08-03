using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Agents.TestKit;

/// <summary>
/// Acceptance-gated deployment target over the real ports: applies through an
/// <see cref="IManagedFileSet"/>, runs a scripted "smoke", consults the supplied
/// <see cref="IAcceptanceEvaluator"/>, and rolls the file set (and a simulated prior
/// image) back on a Rollback verdict. Models the shape of a real install without
/// a container, so a test can assert Ship-vs-Rollback and that Rollback actually
/// restored prior state.
/// </summary>
public sealed class FakeDeploymentTarget : IAcceptanceGatedDeploymentTarget
{
    private readonly FakeManagedFileSet _files;
    private readonly Scripted<DeploymentSmokeResult> _smoke;
    private readonly string _destRoot;

    /// <summary>Creates the target.</summary>
    /// <param name="files">File set to apply through.</param>
    /// <param name="smoke">Scripted smoke outcomes, one per apply.</param>
    /// <param name="destRoot">Destination root passed to the file set.</param>
    public FakeDeploymentTarget(
        FakeManagedFileSet files,
        Scripted<DeploymentSmokeResult>? smoke = null,
        string destRoot = "dest")
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _smoke = smoke ?? Scripted<DeploymentSmokeResult>.Always(DeploymentSmokeResult.NotRun);
        _destRoot = destRoot;
    }

    /// <summary>Artifacts this target was asked to apply, in order.</summary>
    public List<GeneratedArtifact> Applied { get; } = new();

    /// <summary>Acceptance verdicts reached, in order.</summary>
    public List<AcceptanceResult> Verdicts { get; } = new();

    /// <summary>The live image tag, or null once rolled back to the prior tag.</summary>
    public string? CurrentImage { get; private set; }

    /// <summary>The tag treated as the prior/known-good image.</summary>
    public string PriorImage { get; init; } = "image:prev";

    /// <summary>The tag a successful apply publishes.</summary>
    public string NewImage { get; init; } = "image:new";

    /// <summary>True when the prior image was restored by a rollback.</summary>
    public bool PriorImageRestored { get; private set; }

    /// <inheritdoc />
    public Task<DeploymentApplyResult> ApplyAsync(
        GeneratedArtifact artifact,
        CancellationToken cancellationToken = default) =>
        ApplyAsync(artifact, DefaultAcceptanceEvaluator.Instance, cancellationToken);

    /// <inheritdoc />
    public async Task<DeploymentApplyResult> ApplyAsync(
        GeneratedArtifact artifact,
        IAcceptanceEvaluator acceptance,
        CancellationToken cancellationToken = default)
    {
        if (artifact is null) throw new ArgumentNullException(nameof(artifact));
        if (acceptance is null) throw new ArgumentNullException(nameof(acceptance));

        Applied.Add(artifact);

        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["artifact"] = artifact.Content ?? ""
        };
        foreach (var (rel, content) in artifact.Files ?? new Dictionary<string, string>())
            files[rel] = content ?? "";

        var apply = await _files.ApplyAsync(_destRoot, files, cancellationToken).ConfigureAwait(false);
        if (!apply.Succeeded)
        {
            await apply.Rollback(cancellationToken).ConfigureAwait(false);
            return new DeploymentApplyResult { Succeeded = false, Message = "file apply failed" };
        }

        CurrentImage = NewImage;

        var smoke = _smoke.Next();
        var provenance = artifact.Provenance
            ?? GenerativeProvenance.Create(GenerationStrategy.Templated, verified: true);

        var verdict = acceptance.Evaluate(new AcceptanceContext(artifact, provenance, smoke));
        Verdicts.Add(verdict);

        if (verdict.Decision == AcceptanceDecision.Rollback)
        {
            await apply.Rollback(cancellationToken).ConfigureAwait(false);
            CurrentImage = PriorImage;
            PriorImageRestored = true;
            return new DeploymentApplyResult
            {
                Succeeded = false,
                Message = "acceptance rejected the deployment: " + verdict.Reason,
                Destination = _destRoot
            };
        }

        return new DeploymentApplyResult
        {
            Succeeded = true,
            Message = verdict.Reason,
            Destination = _destRoot
        };
    }
}
