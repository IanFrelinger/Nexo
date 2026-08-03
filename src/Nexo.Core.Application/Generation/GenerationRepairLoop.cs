using System.Text;
using Nexo.Core.Application.Orchestration;

namespace Nexo.Core.Application.Generation;

/// <summary>
/// Fixed engine: draft an artifact, run an <see cref="IPostValidator{TOut}"/> chain,
/// feed each failure reason back into the next draft, repeat to a bound.
/// Domain-neutral — no toolchain vocabulary.
/// </summary>
/// <typeparam name="TArtifact">Artifact type produced by the drafter.</typeparam>
public sealed class GenerationRepairLoop<TArtifact>
{
    private readonly Func<IReadOnlyList<ValidationFeedback>, CancellationToken, Task<TArtifact>> _drafter;
    private readonly IReadOnlyList<IPostValidator<TArtifact>> _validators;
    private readonly RepairOptions _options;

    /// <summary>
    /// Creates a repair loop.
    /// </summary>
    /// <param name="drafter">
    /// Produces an artifact given prior validation feedback (empty on first attempt).
    /// </param>
    /// <param name="validators">Post-draft validators; all must pass.</param>
    /// <param name="options">Attempt bound.</param>
    public GenerationRepairLoop(
        Func<IReadOnlyList<ValidationFeedback>, CancellationToken, Task<TArtifact>> drafter,
        IReadOnlyList<IPostValidator<TArtifact>> validators,
        RepairOptions options)
    {
        _drafter = drafter ?? throw new ArgumentNullException(nameof(drafter));
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.MaxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxAttempts must be ≥ 1.");
    }

    /// <summary>Runs draft → validate → repair until success or the attempt bound.</summary>
    public async Task<GenerationRepairResult<TArtifact>> RunAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ValidationFeedback> prior = Array.Empty<ValidationFeedback>();
        TArtifact? lastArtifact = default;
        IReadOnlyList<ValidationFeedback> lastFeedback = Array.Empty<ValidationFeedback>();

        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lastArtifact = await _drafter(prior, cancellationToken).ConfigureAwait(false);

            var feedback = new List<ValidationFeedback>();
            foreach (var validator in _validators)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (ok, reason) = await validator.ValidateAsync(lastArtifact, cancellationToken).ConfigureAwait(false);
                if (!ok)
                {
                    feedback.Add(new ValidationFeedback(
                        Source: validator.GetType().Name,
                        Reason: string.IsNullOrWhiteSpace(reason) ? "validation failed" : reason!));
                }
            }

            if (feedback.Count == 0)
            {
                return new GenerationRepairResult<TArtifact>(
                    Artifact: lastArtifact,
                    Succeeded: true,
                    Attempts: attempt,
                    Feedback: Array.Empty<ValidationFeedback>(),
                    FailureSummary: null);
            }

            lastFeedback = feedback;
            prior = feedback;
        }

        return new GenerationRepairResult<TArtifact>(
            Artifact: lastArtifact,
            Succeeded: false,
            Attempts: _options.MaxAttempts,
            Feedback: lastFeedback,
            FailureSummary: Summarize(lastFeedback));
    }

    private static string Summarize(IReadOnlyList<ValidationFeedback> feedback)
    {
        var sb = new StringBuilder();
        foreach (var item in feedback)
        {
            if (sb.Length > 0) sb.Append("; ");
            sb.Append(item.Source).Append(": ").Append(item.Reason);
        }
        return sb.ToString();
    }
}
