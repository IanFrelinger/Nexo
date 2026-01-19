using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Common;

public sealed class LoopKernelTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSequentialPreservesOrder();
            await TestMaxIterationsStopsEarly();
            await TestCancellationStopsLoop();
            await TestParallelRunsAllIterations();

            return new TestResult
            {
                TestName = nameof(LoopKernelTests),
                Category = "Application.Common",
                Passed = true,
                Message = "All loop kernel tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(LoopKernelTests),
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
                TestName = nameof(LoopKernelTests),
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

        AssertTrue(r.Completed);
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

        AssertTrue(!r.Completed);
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

        AssertTrue(!r.Completed);
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
            AssertEqual(1, seen[i], $"Expected exactly one visit for {i}");
        }
    }
}

