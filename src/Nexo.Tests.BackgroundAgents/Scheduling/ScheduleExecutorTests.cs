using FluentAssertions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Scheduling;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Scheduling;

public class ScheduleExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_Continuous_CallsExecuteOnceMultipleTimes()
    {
        var executor = new ScheduleExecutor();
        var callCount = 0;
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                Schedule = new BackgroundAgentSchedule
                {
                    Type = ScheduleType.Continuous,
                    InitialDelay = TimeSpan.Zero
                }
            },
            State = BackgroundAgentState.Running
        };

        async Task ExecuteOnce(BackgroundAgentInstance i, CancellationToken ct)
        {
            callCount++;
            await Task.Yield();
            if (callCount >= 2)
                i.State = BackgroundAgentState.Stopped;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await executor.ExecuteAsync(instance, ExecuteOnce, cts.Token);

        callCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_Interval_CallsExecuteOnceThenWaits()
    {
        var executor = new ScheduleExecutor();
        var callCount = 0;
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                Schedule = new BackgroundAgentSchedule
                {
                    Type = ScheduleType.Interval,
                    Interval = TimeSpan.FromMilliseconds(50),
                    InitialDelay = TimeSpan.Zero
                }
            },
            State = BackgroundAgentState.Running
        };

        async Task ExecuteOnce(BackgroundAgentInstance i, CancellationToken ct)
        {
            callCount++;
            await Task.Yield();
            if (callCount >= 2)
                i.State = BackgroundAgentState.Stopped;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await executor.ExecuteAsync(instance, ExecuteOnce, cts.Token);

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_Interval_NoInterval_Throws()
    {
        var executor = new ScheduleExecutor();
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                Schedule = new BackgroundAgentSchedule
                {
                    Type = ScheduleType.Interval,
                    Interval = null,
                    InitialDelay = TimeSpan.Zero
                }
            },
            State = BackgroundAgentState.Running
        };

        static Task ExecuteOnce(BackgroundAgentInstance i, CancellationToken ct) => Task.CompletedTask;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var act = () => executor.ExecuteAsync(instance, ExecuteOnce, cts.Token);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*interval*");
    }

    [Fact]
    public async Task ExecuteAsync_Cron_CallsExecuteOnceAfterNextOccurrence()
    {
        var executor = new ScheduleExecutor();
        var callCount = 0;
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                Schedule = new BackgroundAgentSchedule
                {
                    Type = ScheduleType.Cron,
                    CronExpression = "* * * * *",
                    InitialDelay = TimeSpan.Zero
                }
            },
            State = BackgroundAgentState.Running
        };

        async Task ExecuteOnce(BackgroundAgentInstance i, CancellationToken ct)
        {
            callCount++;
            await Task.Yield();
            i.State = BackgroundAgentState.Stopped;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(65));
        await executor.ExecuteAsync(instance, ExecuteOnce, cts.Token);

        callCount.Should().Be(1);
    }
}
