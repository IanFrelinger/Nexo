using FluentAssertions;
using Nexo.Core.Domain.Execution;
using Nexo.GameDomain;

namespace Nexo.Tests.GameDomain;

public sealed class GameplayResultAggregatorTests
{
    [Fact]
    public void FirstSuccess_PicksFastestPass()
    {
        var slow = new GameplayCandidateResult(
            "a",
            succeeded: true,
            output: new BrickOutput(),
            duration: TimeSpan.FromMilliseconds(40));
        var fast = new GameplayCandidateResult(
            "b",
            succeeded: true,
            output: new BrickOutput(),
            duration: TimeSpan.FromMilliseconds(5));
        var failed = new GameplayCandidateResult(
            "c",
            succeeded: false,
            error: "boom",
            duration: TimeSpan.FromMilliseconds(1));

        var result = GameplayResultAggregator.Aggregate(
            new GameplayAggregateRequest([slow, fast, failed], GameplayJoinStrategy.FirstSuccess));

        result.Joined.Should().BeTrue();
        result.SuccessCount.Should().Be(2);
        result.Best!.BrickId.Should().Be("b");
    }

    [Fact]
    public void All_RequiresEveryCandidate()
    {
        var ok = new GameplayCandidateResult("a", true, new BrickOutput());
        var fail = new GameplayCandidateResult("b", false, error: "x");

        var result = GameplayResultAggregator.Aggregate(
            new GameplayAggregateRequest([ok, fail], GameplayJoinStrategy.All));

        result.Joined.Should().BeFalse();
    }

    [Fact]
    public void Quorum_NeedsConfiguredSuccesses()
    {
        var results = new[]
        {
            new GameplayCandidateResult("a", true, new BrickOutput()),
            new GameplayCandidateResult("b", true, new BrickOutput()),
            new GameplayCandidateResult("c", false, error: "x")
        };

        GameplayResultAggregator.Aggregate(
                new GameplayAggregateRequest(results, GameplayJoinStrategy.Quorum, quorum: 2))
            .Joined.Should().BeTrue();

        GameplayResultAggregator.Aggregate(
                new GameplayAggregateRequest(results, GameplayJoinStrategy.Quorum, quorum: 3))
            .Joined.Should().BeFalse();
    }
}
