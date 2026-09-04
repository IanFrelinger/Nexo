using System.Diagnostics;
using Ashlar.Core.Application.Execution.Ports;
using Ashlar.Infrastructure.HostProcess;
using Ashlar.Infrastructure.Scaling;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.ProcessExecution;

/// <summary>
/// TimedProcess must return on a wall-clock ceiling and kill the child tree.
/// An unbounded WaitForExit against docker is what froze local full runs.
/// </summary>
public sealed class TimedProcessTests
{
    [Fact]
    public void OperatorAndDockerCeilings_AreBounded()
    {
        TimedProcess.OperatorCommandTimeout.Should().Be(TimeSpan.FromHours(2));
        TimedProcess.DockerWaitTimeout.Should().Be(TimeSpan.FromMinutes(30));
        TimedProcess.OperatorCommandTimeout.Should().BeGreaterThan(TimeSpan.Zero);
        TimedProcess.DockerWaitTimeout.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task RunAsync_TimeoutKillsProcessTree()
    {
        var sw = Stopwatch.StartNew();
        var result = await TimedProcess.RunAsync(LongSleepStartInfo(), TimeSpan.FromSeconds(1));
        sw.Stop();

        result.TimedOut.Should().BeTrue();
        result.ExitCode.Should().Be(TimedProcess.TimeoutExitCode);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(8));
    }

    [Fact]
    public async Task RunAsync_CompletesBeforeTimeout()
    {
        var psi = new ProcessStartInfo();
        if (OperatingSystem.IsWindows())
        {
            psi.FileName = "cmd.exe";
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add("echo hello");
        }
        else
        {
            psi.FileName = "echo";
            psi.ArgumentList.Add("hello");
        }

        var result = await TimedProcess.RunAsync(psi, TimeSpan.FromSeconds(8));

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(0);
        result.StdOut.Should().Contain("hello");
    }

    [Fact]
    public async Task RunAsync_CallerCancelKillsProcessTreeAndThrows()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        var sw = Stopwatch.StartNew();

        var act = async () => await TimedProcess.RunAsync(
            LongSleepStartInfo(),
            Timeout.InfiniteTimeSpan,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(8));
    }

    [Fact]
    public async Task ProcessCommandRunner_CancelKillsProcessTree()
    {
        var runner = new ProcessCommandRunner();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        var sw = Stopwatch.StartNew();

        Func<Task<ProcessCommandResult>> act = OperatingSystem.IsWindows()
            ? () => runner.RunAsync(
                "powershell.exe",
                new[] { "-NoProfile", "-Command", "Start-Sleep -Seconds 30" },
                cts.Token)
            : () => runner.RunAsync("sleep", new[] { "30" }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(8));
    }

    private static ProcessStartInfo LongSleepStartInfo()
    {
        if (OperatingSystem.IsWindows())
        {
            var psi = new ProcessStartInfo { FileName = "powershell.exe" };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add("Start-Sleep -Seconds 30");
            return psi;
        }

        var unix = new ProcessStartInfo { FileName = "sleep" };
        unix.ArgumentList.Add("30");
        return unix;
    }
}
