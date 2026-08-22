using Ashlar.Core.Domain.Bricks.Ports;

namespace Ashlar.Agents.TestKit;

/// <summary>
/// Scripted <see cref="IArtifactDrafter"/> over the real port. Records the feedback
/// it was handed on each attempt, so a test can assert the repair loop actually fed
/// the prior failure back rather than redrafting blind.
/// </summary>
public sealed class FakeArtifactDrafter : IArtifactDrafter
{
    private readonly Scripted<GeneratedArtifact> _drafts;

    /// <summary>Creates a drafter from a scripted run of drafts.</summary>
    public FakeArtifactDrafter(Scripted<GeneratedArtifact> drafts) =>
        _drafts = drafts ?? throw new ArgumentNullException(nameof(drafts));

    /// <summary>Creates a drafter from an explicit run of drafts.</summary>
    public FakeArtifactDrafter(params GeneratedArtifact[] drafts)
        : this(new Scripted<GeneratedArtifact>(drafts)) { }

    /// <summary>Creates a drafter from content strings.</summary>
    public static FakeArtifactDrafter OfContent(params string[] contents) =>
        new(contents.Select(c => new GeneratedArtifact { Content = c }).ToArray());

    /// <summary>
    /// The repair-from-empty script: the model returns nothing first, then a real
    /// artifact once it has been told what was wrong.
    /// </summary>
    public static FakeArtifactDrafter EmptyThen(string content) =>
        OfContent("", content);

    /// <summary>Feedback handed to each attempt, in order.</summary>
    public List<IReadOnlyList<string>> FeedbackPerAttempt { get; } = new();

    /// <summary>Requests seen, in order.</summary>
    public List<GenerationRequest> Requests { get; } = new();

    /// <summary>Number of draft attempts.</summary>
    public int Attempts => _drafts.Calls;

    /// <inheritdoc />
    public Task<GeneratedArtifact> DraftAsync(
        GenerationRequest request,
        IReadOnlyList<string> priorFeedback,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        FeedbackPerAttempt.Add(priorFeedback?.ToArray() ?? Array.Empty<string>());
        return Task.FromResult(_drafts.Next());
    }
}

/// <summary>
/// Scripted <see cref="IDeterministicDrafter"/>. A profile that declares
/// SupportsDeterministic must produce an artifact with no model in the loop; this
/// fake stands in for that path in offline runs.
/// </summary>
public sealed class FakeDeterministicDrafter : IDeterministicDrafter
{
    private readonly Scripted<GeneratedArtifact?> _drafts;

    /// <summary>Creates a deterministic drafter from a scripted run.</summary>
    public FakeDeterministicDrafter(Scripted<GeneratedArtifact?> drafts) =>
        _drafts = drafts ?? throw new ArgumentNullException(nameof(drafts));

    /// <summary>Creates a drafter that always produces the given content.</summary>
    public FakeDeterministicDrafter(string content)
        : this(Scripted<GeneratedArtifact?>.Always(new GeneratedArtifact { Content = content })) { }

    /// <summary>Creates a drafter that always declines the deterministic path.</summary>
    public static FakeDeterministicDrafter Declining() =>
        new(Scripted<GeneratedArtifact?>.Always(null));

    /// <summary>Number of TryDraft calls.</summary>
    public int Calls => _drafts.Calls;

    /// <inheritdoc />
    public bool TryDraft(GenerationRequest request, out GeneratedArtifact? artifact)
    {
        artifact = _drafts.Next();
        return artifact is not null;
    }
}

/// <summary>
/// Scripted <see cref="IAcceptanceEvaluator"/> for driving deploy-flow branches
/// without needing a real evaluator's input shape.
/// </summary>
public sealed class FakeAcceptanceEvaluator : IAcceptanceEvaluator
{
    private readonly Scripted<AcceptanceResult> _verdicts;

    /// <summary>Creates an evaluator from a scripted run of verdicts.</summary>
    public FakeAcceptanceEvaluator(Scripted<AcceptanceResult> verdicts) =>
        _verdicts = verdicts ?? throw new ArgumentNullException(nameof(verdicts));

    /// <summary>Creates an evaluator that always returns the given verdict.</summary>
    public FakeAcceptanceEvaluator(AcceptanceResult verdict)
        : this(Scripted<AcceptanceResult>.Always(verdict)) { }

    /// <summary>Always rolls back with the given reason.</summary>
    public static FakeAcceptanceEvaluator AlwaysRollback(string reason = "scripted rollback") =>
        new(AcceptanceResult.Rollback(reason));

    /// <summary>Always ships with the given reason.</summary>
    public static FakeAcceptanceEvaluator AlwaysShip(string reason = "scripted ship") =>
        new(AcceptanceResult.Ship(reason));

    /// <summary>Contexts this evaluator was asked to judge, in order.</summary>
    public List<AcceptanceContext> Seen { get; } = new();

    /// <summary>Number of evaluations.</summary>
    public int Calls => _verdicts.Calls;

    /// <inheritdoc />
    public AcceptanceResult Evaluate(AcceptanceContext context)
    {
        Seen.Add(context);
        return _verdicts.Next();
    }
}
