using System.Net.Http;
using FluentAssertions;
using Nexo.Core.Application.Resilience.Ports;
using Xunit;

namespace Nexo.Tests.Application.Tests.Resilience;

/// <summary>
/// Pins <see cref="RetryPolicy.DelayForAttempt"/> — the single backoff
/// calculation, absorbed from the deleted second RetryPolicy type.
///
/// The point of these is regression safety for a merge: the Fixed / Linear /
/// Exponential / Jittered shapes previously lived in
/// <c>Nexo.Orchestration.Resilience.RetryPolicy.CalculateDelay</c>, and the
/// executor had its own inline exponential. Both are now this one method, so
/// these tests assert the folded maths still matches what each side computed.
/// </summary>
public sealed class RetryPolicyBackoffTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 100)]
    [InlineData(5, 100)]
    public void Fixed_returns_the_base_delay_for_every_attempt(int attempt, double expectedMs)
    {
        var policy = RetryPolicy.FixedDelay(5, TimeSpan.FromMilliseconds(100));

        policy.DelayForAttempt(attempt).TotalMilliseconds.Should().Be(expectedMs);
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    [InlineData(3, 300)]
    public void Linear_scales_with_the_attempt_number(int attempt, double expectedMs)
    {
        var policy = new RetryPolicy(
            MaxAttempts: 5,
            BaseDelay: TimeSpan.FromMilliseconds(100),
            Backoff: RetryBackoff.Linear);

        policy.DelayForAttempt(attempt).TotalMilliseconds.Should().Be(expectedMs);
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    [InlineData(3, 400)]
    [InlineData(4, 800)]
    public void Exponential_doubles_by_default(int attempt, double expectedMs)
    {
        // This is the shape the executor computed inline as
        // BaseDelay * 2^(attempt-1) before the merge — same numbers.
        var policy = new RetryPolicy(MaxAttempts: 5, BaseDelay: TimeSpan.FromMilliseconds(100));

        policy.DelayForAttempt(attempt).TotalMilliseconds.Should().Be(expectedMs);
    }

    [Fact]
    public void Exponential_honours_a_custom_multiplier()
    {
        var policy = new RetryPolicy(
            MaxAttempts: 5,
            BaseDelay: TimeSpan.FromMilliseconds(100),
            BackoffMultiplier: 3.0);

        policy.DelayForAttempt(3).TotalMilliseconds.Should().Be(900);
    }

    [Fact]
    public void MaxDelay_caps_the_computed_delay()
    {
        var policy = new RetryPolicy(
            MaxAttempts: 10,
            BaseDelay: TimeSpan.FromMilliseconds(100),
            MaxDelay: TimeSpan.FromMilliseconds(500));

        policy.DelayForAttempt(1).TotalMilliseconds.Should().Be(100);
        policy.DelayForAttempt(9).TotalMilliseconds.Should().Be(500, "the cap must bind once growth exceeds it");
    }

    [Fact]
    public void No_MaxDelay_means_uncapped()
    {
        // Deliberate: the executor was uncapped before the merge, and RetryPolicy.Default
        // must keep behaving exactly as it did.
        var policy = new RetryPolicy(MaxAttempts: 10, BaseDelay: TimeSpan.FromMilliseconds(100));

        policy.DelayForAttempt(9).TotalMilliseconds.Should().Be(100 * Math.Pow(2, 8));
    }

    [Fact]
    public void Jittered_stays_within_half_to_full_of_the_exponential_value()
    {
        var policy = new RetryPolicy(
            MaxAttempts: 5,
            BaseDelay: TimeSpan.FromMilliseconds(1000),
            Backoff: RetryBackoff.JitteredExponential);

        // attempt 3 => 1000 * 2^2 = 4000, jittered into [2000, 4000).
        for (var i = 0; i < 200; i++)
        {
            var ms = policy.DelayForAttempt(3).TotalMilliseconds;
            ms.Should().BeGreaterThanOrEqualTo(2000).And.BeLessThanOrEqualTo(4000);
        }
    }

    [Fact]
    public void Jittered_actually_varies()
    {
        // A jitter implementation that quietly stopped jittering would still pass
        // the range check above; this catches that.
        var policy = new RetryPolicy(
            MaxAttempts: 5,
            BaseDelay: TimeSpan.FromMilliseconds(1000),
            Backoff: RetryBackoff.JitteredExponential);

        var seen = Enumerable.Range(0, 50)
            .Select(_ => policy.DelayForAttempt(2).TotalMilliseconds)
            .Distinct()
            .Count();

        seen.Should().BeGreaterThan(1, "jitter that never varies is not jitter");
    }

    [Fact]
    public void Attempt_numbering_is_one_based()
    {
        var policy = RetryPolicy.Default;

        var act = () => policy.DelayForAttempt(0);

        act.Should().Throw<ArgumentOutOfRangeException>(
            "attempt 1 is the first try, so 0 is a caller bug rather than a silent zero delay");
    }

    [Fact]
    public void Default_policy_is_unchanged_by_the_merge()
    {
        RetryPolicy.Default.MaxAttempts.Should().Be(3);
        RetryPolicy.Default.BaseDelay.Should().Be(TimeSpan.FromSeconds(1));
        RetryPolicy.Default.Backoff.Should().Be(RetryBackoff.Exponential);
        RetryPolicy.Default.BackoffMultiplier.Should().Be(2.0);
        RetryPolicy.Default.MaxDelay.Should().BeNull();

        // Unset IsTransient still resolves to the network classifier.
        var resolved = RetryPolicy.Default.ResolveIsTransient();
        resolved(new HttpRequestException()).Should().BeTrue();
        resolved(new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public void FixedDelay_factory_carries_its_classifier()
    {
        static bool OnlyTimeouts(Exception ex) => ex is TimeoutException;

        var policy = RetryPolicy.FixedDelay(3, TimeSpan.FromMilliseconds(1), OnlyTimeouts);

        policy.Backoff.Should().Be(RetryBackoff.Fixed);
        policy.MaxAttempts.Should().Be(3);
        policy.ResolveIsTransient()(new TimeoutException()).Should().BeTrue();
        policy.ResolveIsTransient()(new InvalidOperationException()).Should().BeFalse();
    }
}
