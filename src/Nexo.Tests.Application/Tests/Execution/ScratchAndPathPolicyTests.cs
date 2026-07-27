using FluentAssertions;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Execution.Scratch;
using Xunit;

namespace Nexo.Tests.Application.Tests.Execution;

public sealed class ScratchAndPathPolicyTests
{
    [Fact]
    public void ScratchDir_IsCreated_AndDisposed()
    {
        var space = new FileScratchSpace();
        string path;
        using (var dir = space.CreateScratchDir("unit"))
        {
            path = dir.Path;
            Directory.Exists(path).Should().BeTrue();
            File.WriteAllText(System.IO.Path.Combine(path, "a.txt"), "x");
        }

        Directory.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void WorkspacePathPolicy_NoOpContainment_WhenRootUnset()
    {
        var previous = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
            var policy = new WorkspacePathPolicy();
            policy.IsUnderRoot(@"C:\anywhere\file.txt").Should().BeTrue();
            var act = () => policy.EnsureContained(@"C:\anywhere\file.txt");
            act.Should().NotThrow();
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", previous);
        }
    }

    [Fact]
    public async Task SingleFlightGuard_SerializesSameKey()
    {
        var guard = new SingleFlightGuard();
        var concurrent = 0;
        var max = 0;
        var tasks = Enumerable.Range(0, 5).Select(_ =>
            guard.RunExclusiveAsync("k", async ct =>
            {
                var now = Interlocked.Increment(ref concurrent);
                Interlocked.Exchange(ref max, Math.Max(max, now));
                await Task.Delay(20, ct);
                Interlocked.Decrement(ref concurrent);
                return 1;
            }, CancellationToken.None));

        await Task.WhenAll(tasks);
        max.Should().Be(1);
    }
}
