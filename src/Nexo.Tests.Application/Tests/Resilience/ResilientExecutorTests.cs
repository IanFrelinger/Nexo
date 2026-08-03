using FluentAssertions;
using Nexo.Core.Application.Resilience.Ports;
using Nexo.Infrastructure.Resilience;
using Xunit;

namespace Nexo.Tests.Application.Tests.Resilience;

/// <summary>
/// Step 1: IResilientExecutor — retry with a fake transient classifier,
/// never retry on caller cancellation, default TransientClassifiers.Network.
/// </summary>
public sealed class ResilientExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_Succeeds_OnFirstAttempt()
    {
        var executor = new ResilientExecutor();
        var calls = 0;

        var result = await executor.ExecuteAsync(
            _ =>
            {
                calls++;
                return Task.FromResult(42);
            },
            new RetryPolicy(MaxAttempts: 3, BaseDelay: TimeSpan.FromMilliseconds(1), IsTransient: _ => true),
            CancellationToken.None);

        result.Should().Be(42);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_Retries_OnFakeTransient_ThenSucceeds()
    {
        var executor = new ResilientExecutor();
        var calls = 0;

        var result = await executor.ExecuteAsync(
            _ =>
            {
                calls++;
                if (calls < 3)
                    throw new InvalidOperationException("transient-for-test");
                return Task.FromResult("ok");
            },
            new RetryPolicy(
                MaxAttempts: 5,
                BaseDelay: TimeSpan.FromMilliseconds(1),
                IsTransient: ex => ex is InvalidOperationException),
            CancellationToken.None);

        result.Should().Be("ok");
        calls.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetry_WhenIsTransientReturnsFalse()
    {
        var executor = new ResilientExecutor();
        var calls = 0;

        var act = () => executor.ExecuteAsync<int>(
            _ =>
            {
                calls++;
                throw new InvalidOperationException("permanent");
            },
            new RetryPolicy(MaxAttempts: 5, BaseDelay: TimeSpan.FromMilliseconds(1), IsTransient: _ => false),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        calls.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_Throws_AfterMaxAttempts()
    {
        var executor = new ResilientExecutor();
        var calls = 0;

        var act = () => executor.ExecuteAsync<int>(
            _ =>
            {
                calls++;
                throw new IOException("still down");
            },
            new RetryPolicy(MaxAttempts: 3, BaseDelay: TimeSpan.FromMilliseconds(1), IsTransient: TransientClassifiers.Network),
            CancellationToken.None);

        await act.Should().ThrowAsync<IOException>();
        calls.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_NeverRetries_WhenCallerTokenCancelled()
    {
        var executor = new ResilientExecutor();
        using var cts = new CancellationTokenSource();
        var calls = 0;

        var act = () => executor.ExecuteAsync<int>(
            ct =>
            {
                calls++;
                if (calls == 1)
                {
                    cts.Cancel();
                    throw new OperationCanceledException(ct);
                }

                return Task.FromResult(0);
            },
            new RetryPolicy(MaxAttempts: 5, BaseDelay: TimeSpan.FromMilliseconds(1), IsTransient: _ => true),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        calls.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_FailsFast_WhenTokenAlreadyCancelled()
    {
        var executor = new ResilientExecutor();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var calls = 0;

        var act = () => executor.ExecuteAsync<int>(
            _ =>
            {
                calls++;
                return Task.FromResult(0);
            },
            new RetryPolicy(MaxAttempts: 5, BaseDelay: TimeSpan.FromMilliseconds(1), IsTransient: _ => true),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        calls.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_DefaultsIsTransient_ToNetworkClassifier()
    {
        var executor = new ResilientExecutor();
        var calls = 0;

        var act = () => executor.ExecuteAsync<int>(
            _ =>
            {
                calls++;
                throw new HttpRequestException("connection reset");
            },
            new RetryPolicy(MaxAttempts: 2, BaseDelay: TimeSpan.FromMilliseconds(1)),
            CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        calls.Should().Be(2);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException), true)]
    [InlineData(typeof(IOException), true)]
    [InlineData(typeof(TimeoutException), true)]
    [InlineData(typeof(InvalidOperationException), false)]
    [InlineData(typeof(ArgumentException), false)]
    public void TransientClassifiers_Network_ClassifiesBclTransportFailures(Type exceptionType, bool expected)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType, "test")!;
        TransientClassifiers.Network(ex).Should().Be(expected);
    }

    [Fact]
    public void TransientClassifiers_Network_TreatsSocketExceptionAsTransient()
    {
        TransientClassifiers.Network(new System.Net.Sockets.SocketException()).Should().BeTrue();
    }
}
