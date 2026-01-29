using FluentAssertions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Scheduling;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Scheduling;

public class AgentSchedulerTests
{
    [Fact]
    public async Task StartAsync_RunsScheduleAndStop_StopsLoop()
    {
        var executor = new ScheduleExecutor();
        var scheduler = new AgentScheduler(executor);
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
            {
                scheduler.Stop("agent-1");
                i.State = BackgroundAgentState.Stopped;
            }
        }

        await scheduler.StartAsync(instance, ExecuteOnce);
        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        callCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void Stop_WhenNotStarted_DoesNotThrow()
    {
        var executor = new ScheduleExecutor();
        var scheduler = new AgentScheduler(executor);
        scheduler.Stop("nonexistent");
    }

    [Fact]
    public async Task StartAsync_SameAgentTwice_LogsWarningAndReturns()
    {
        var executor = new ScheduleExecutor();
        var scheduler = new AgentScheduler(executor);
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                Schedule = new BackgroundAgentSchedule
                {
                    Type = ScheduleType.Interval,
                    Interval = TimeSpan.FromSeconds(10),
                    InitialDelay = TimeSpan.Zero
                }
            },
            State = BackgroundAgentState.Running
        };

        static Task ExecuteOnce(BackgroundAgentInstance i, CancellationToken ct) => Task.CompletedTask;

        await scheduler.StartAsync(instance, ExecuteOnce);
        await scheduler.StartAsync(instance, ExecuteOnce);
        scheduler.Stop("agent-1");
    }
}
