using FluentAssertions;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Orchestration;
using Xunit;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Step 3: GenerationRepairLoop — draft → IPostValidator chain → redraft on feedback.
/// </summary>
public sealed class GenerationRepairLoopTests
{
    [Fact]
    public async Task RunAsync_Succeeds_OnFirstDraft_WhenValidatorsPass()
    {
        var drafts = 0;
        var loop = new GenerationRepairLoop<string>(
            (prior, _) =>
            {
                drafts++;
                prior.Should().BeEmpty();
                return Task.FromResult("artifact-v1");
            },
            new IPostValidator<string>[] { new PassValidator() },
            new RepairOptions(MaxAttempts: 3));

        var result = await loop.RunAsync(CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Artifact.Should().Be("artifact-v1");
        result.Attempts.Should().Be(1);
        result.Feedback.Should().BeEmpty();
        drafts.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_Redrafts_FeedingValidatorReasons()
    {
        var drafts = 0;
        IReadOnlyList<ValidationFeedback>? lastPrior = null;
        var loop = new GenerationRepairLoop<string>(
            (prior, _) =>
            {
                drafts++;
                lastPrior = prior;
                return Task.FromResult(drafts == 1 ? "bad" : "good");
            },
            new IPostValidator<string>[] { new FailUntilGoodValidator() },
            new RepairOptions(MaxAttempts: 3));

        var result = await loop.RunAsync(CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Artifact.Should().Be("good");
        result.Attempts.Should().Be(2);
        drafts.Should().Be(2);
        lastPrior.Should().NotBeNull();
        lastPrior!.Should().ContainSingle(f => f.Reason.Contains("expected good", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_AggregatesFeedback_FromMultipleValidators()
    {
        var loop = new GenerationRepairLoop<string>(
            (_, _) => Task.FromResult("x"),
            new IPostValidator<string>[]
            {
                new FailValidator("reason-a"),
                new FailValidator("reason-b")
            },
            new RepairOptions(MaxAttempts: 1));

        var result = await loop.RunAsync(CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Feedback.Should().HaveCount(2);
        result.Feedback.Select(f => f.Reason).Should().Contain(new[] { "reason-a", "reason-b" });
        result.FailureSummary.Should().Contain("reason-a");
    }

    [Fact]
    public async Task RunAsync_Stops_AfterMaxAttempts()
    {
        var drafts = 0;
        var loop = new GenerationRepairLoop<string>(
            (_, _) =>
            {
                drafts++;
                return Task.FromResult("still-bad");
            },
            new IPostValidator<string>[] { new FailValidator("nope") },
            new RepairOptions(MaxAttempts: 3));

        var result = await loop.RunAsync(CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Attempts.Should().Be(3);
        drafts.Should().Be(3);
        result.Artifact.Should().Be("still-bad");
    }

    [Fact]
    public async Task RunAsync_DoesNotCallValidators_WhenCancelledBeforeDraft()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var validated = false;
        var loop = new GenerationRepairLoop<string>(
            (_, _) => Task.FromResult("x"),
            new IPostValidator<string>[]
            {
                new DelegateValidator((_, _) =>
                {
                    validated = true;
                    return ValueTask.FromResult<(bool, string?)>((true, null));
                })
            },
            new RepairOptions(MaxAttempts: 2));

        var act = () => loop.RunAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        validated.Should().BeFalse();
    }

    private sealed class PassValidator : IPostValidator<string>
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(string output, CancellationToken ct) =>
            ValueTask.FromResult<(bool, string?)>((true, null));
    }

    private sealed class FailValidator : IPostValidator<string>
    {
        private readonly string _reason;
        public FailValidator(string reason) => _reason = reason;
        public ValueTask<(bool ok, string? reason)> ValidateAsync(string output, CancellationToken ct) =>
            ValueTask.FromResult<(bool, string?)>((false, _reason));
    }

    private sealed class FailUntilGoodValidator : IPostValidator<string>
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(string output, CancellationToken ct) =>
            output == "good"
                ? ValueTask.FromResult<(bool, string?)>((true, null))
                : ValueTask.FromResult<(bool, string?)>((false, "expected good"));
    }

    private sealed class DelegateValidator : IPostValidator<string>
    {
        private readonly Func<string, CancellationToken, ValueTask<(bool, string?)>> _fn;
        public DelegateValidator(Func<string, CancellationToken, ValueTask<(bool, string?)>> fn) => _fn = fn;
        public ValueTask<(bool ok, string? reason)> ValidateAsync(string output, CancellationToken ct) =>
            _fn(output, ct);
    }
}
