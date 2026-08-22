using Ashlar.Core.Application.Common.Ports;
using Ashlar.Core.Application.Common.Services;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Application.Tests.Common;

/// <summary>Tests for loop kernel.</summary>
public sealed class LoopKernelTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test sequential preserves order.</summary>
            await TestSequentialPreservesOrder();
            /// <summary>Test max iterations stops early.</summary>
            await TestMaxIterationsStopsEarly();
            /// <summary>Test cancellation stops loop.</summary>
            await TestCancellationStopsLoop();
            /// <summary>Test parallel runs all iterations.</summary>
            await TestParallelRunsAllIterations();

            return new TestResult
            {
                Name = nameof(LoopKernelTests),
                Category = "Application.Common",
                Passed = true,
                Message = "All loop kernel tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(LoopKernelTests),
                Category = "Application.Common",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(LoopKernelTests),
                Category = "Application.Common",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private Task TestSequentialPreservesOrder()
    {
        var loop = new SequentialLoopKernel();
        var items = new[] { 1, 2, 3, 4 };
        var seen = new List<int>();

        var r = loop.ForEach(items, (x, _, _) =>
        {
            seen.Add(x);
            return LoopAction.Continue;
        }, new LoopOptions { Name = "seq-order" }, CancellationToken.None);

        /// <summary>Assert true.</summary>
        AssertTrue(r.Completed);
        /// <summary>Assert equal.</summary>
        AssertEqual(4, r.Iterations);
        AssertEqual("1,2,3,4", string.Join(",", seen));
        return Task.CompletedTask;
    }

    private Task TestMaxIterationsStopsEarly()
    {
        var loop = new SequentialLoopKernel();
        var items = new[] { 1, 2, 3, 4 };
        var seen = new List<int>();

        var r = loop.ForEach(items, (x, _, _) =>
        {
            seen.Add(x);
            return LoopAction.Continue;
        }, new LoopOptions { Name = "max-iter", MaxIterations = 2 }, CancellationToken.None);

        /// <summary>Assert true.</summary>
        AssertTrue(!r.Completed);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, r.Iterations);
        AssertEqual("1,2", string.Join(",", seen));
        return Task.CompletedTask;
    }

    private async Task TestCancellationStopsLoop()
    {
        var loop = new SequentialLoopKernel();
        var items = new[] { 1, 2, 3, 4 };
        var seen = new List<int>();

        using var cts = new CancellationTokenSource();

        var r = await loop.ForEachAsync(items, (x, _, ct) =>
        {
            seen.Add(x);
            if (x == 1) cts.Cancel();
            return new ValueTask<LoopAction>(LoopAction.Continue);
        }, new LoopOptions { Name = "cancel" }, cts.Token);

        /// <summary>Assert true.</summary>
        AssertTrue(!r.Completed);
        /// <summary>Assert true.</summary>
        AssertTrue(r.Cancelled);
        AssertEqual("1", string.Join(",", seen));
    }

    private async Task TestParallelRunsAllIterations()
    {
        ILoopKernel loop = new ParallelLoopKernel(new SequentialLoopKernel());
        var items = Enumerable.Range(0, 50).ToArray();
        var seen = new int[items.Length];

        await loop.ForEachAsync(items, (x, _, _) =>
        {
            Interlocked.Increment(ref seen[x]);
            return new ValueTask<LoopAction>(LoopAction.Continue);
        }, new LoopOptions { Name = "parallel", EnableParallel = true, MaxDegreeOfParallelism = 8 }, CancellationToken.None);

        for (var i = 0; i < seen.Length; i++)
        {
            /// <summary>Assert equal.</summary>
            /// <param name="{i}"">{i}".</param>
            AssertEqual(1, seen[i], $"Expected exactly one visit for {i}");
        }
    }
}

