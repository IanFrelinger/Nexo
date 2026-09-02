using System.Text.Json;
using FluentAssertions;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// The defect: <c>ashlar background-agent daemon</c> exited 0 and reported
/// <c>ok:true / reason:"duration_elapsed"</c> after the agent service crashed at startup and ran
/// zero cycles.
///
/// <para>When a hosted service faults, .NET's default
/// <c>BackgroundServiceExceptionBehavior.StopHost</c> logs it and tears the host down —
/// <c>host.StartAsync</c> has already returned by then, so the command sat in its duration delay,
/// woke up and reported a clean, complete window. Under <c>--format-json</c> it emitted
/// <c>ok:true / status:running</c> AFTER shutdown had begun, then
/// <c>ok:true / status:stopped / reason:"duration_elapsed"</c>: two false statements about the same
/// two seconds of a fifteen-second window.</para>
///
/// <para>These pin the JUDGEMENT the report now carries. The wiring that produces the inputs — the
/// <c>ApplicationStopping</c> race and the registry read — is a handful of lines in
/// <c>RunOnceAsync</c>; what used to be wrong was not the wiring but the conclusion drawn with no
/// inputs at all.</para>
/// </summary>
public sealed class DaemonStopReportTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private static DaemonFaultLog FaultedLog()
    {
        var log = new DaemonFaultLog();
        log.Capture(
            "Ashlar.BackgroundAgents.Services.BackgroundAgentService",
            "Background agent service failed: role 'extendr' is not a known role",
            new InvalidOperationException("role 'extendr' is not a known role"));
        return log;
    }

    // ── the inverse defect: an honest stop reported as a failure ───────────────────────────
    //
    // The fix above ("the host stopped itself ⇒ faulted") over-reached. ConsoleLifetime answers
    // SIGTERM and Ctrl+C by calling StopApplication, so ApplicationStopping fires on an ordinary
    // `docker stop` exactly as it does on a crash. Measured: unbounded + SIGTERM exited 0 with
    // reason "shutdown", while --duration + SIGTERM exited 1 with ok:false / status:faulted /
    // reason:host_stopped_early and wrote "faulted" into the heartbeat the HEALTHCHECK reads.
    // The guard meant to tell them apart was dead code: the handler passed no cancellation token.

    [Fact]
    public void An_operator_stop_of_a_bounded_run_is_a_clean_stop_not_a_fault()
    {
        BackgroundAgentDaemonCommand.ClassifyBoundedStop(
            operatorStopped: true, hostStopping: true, faulted: false)
            .Should().Be(BackgroundAgentDaemonCommand.BoundedStopVerdict.CleanStop,
                "a `docker stop` of a --duration run is the same event as a `docker stop` of an unbounded one, "
                + "and the unbounded one has always exited 0 with reason 'shutdown'");
    }

    [Fact]
    public void A_host_that_stopped_with_nothing_logged_is_a_clean_stop()
    {
        // The token was not observed at all before the fix, so this is the shape that actually
        // occurred in production: ApplicationStopping fired, nothing failed, and the daemon
        // called it a fault anyway.
        BackgroundAgentDaemonCommand.ClassifyBoundedStop(
            operatorStopped: false, hostStopping: true, faulted: false)
            .Should().Be(BackgroundAgentDaemonCommand.BoundedStopVerdict.CleanStop,
                "a fault is a fault because something FAILED, not because the host stopped");
    }

    [Fact]
    public void A_host_that_stopped_after_a_service_logged_a_failure_is_still_a_fault()
    {
        // The original defect must stay closed: this is the crashed-at-startup case that used to
        // report ok:true / duration_elapsed.
        BackgroundAgentDaemonCommand.ClassifyBoundedStop(
            operatorStopped: false, hostStopping: true, faulted: true)
            .Should().Be(BackgroundAgentDaemonCommand.BoundedStopVerdict.Faulted);
    }

    [Fact]
    public void A_window_that_simply_elapsed_is_neither()
    {
        BackgroundAgentDaemonCommand.ClassifyBoundedStop(
            operatorStopped: false, hostStopping: false, faulted: false)
            .Should().Be(BackgroundAgentDaemonCommand.BoundedStopVerdict.WindowElapsed);
    }

    [Fact]
    public void A_clean_stop_reports_ok_true_and_the_same_reason_an_unbounded_run_reports()
    {
        var json = Parse(BackgroundAgentDaemonCommand.StoppedJson(
            "shutdown", cycles: 3, window: TimeSpan.FromMinutes(5)));

        json.GetProperty("ok").GetBoolean().Should().BeTrue();
        json.GetProperty("status").GetString().Should().Be("stopped");
        json.GetProperty("reason").GetString().Should().Be("shutdown",
            "the heartbeat and the HEALTHCHECK read this; 'faulted' on an honest stop is a false alarm "
            + "an operator has to chase");
    }

    [Fact]
    public void A_host_that_faulted_is_not_ok_and_names_the_service_and_the_reason()
    {
        var json = Parse(BackgroundAgentDaemonCommand.HostStoppedItselfJson(
            FaultedLog(), ran: TimeSpan.FromSeconds(2), window: TimeSpan.FromSeconds(15), cycles: 0));

        json.GetProperty("ok").GetBoolean().Should().BeFalse(
            "this is the exact reported shape: ok:true over a host that had been dead for 13 of 15 seconds");
        json.GetProperty("status").GetString().Should().Be("faulted");
        json.GetProperty("reason").GetString().Should().Be("background_service_faulted",
            "\"duration_elapsed\" was factually false — the window did not elapse");
        json.GetProperty("service").GetString().Should().Be("Ashlar.BackgroundAgents.Services.BackgroundAgentService");
        json.GetProperty("error").GetString().Should().Contain("not a known role");
        json.GetProperty("cycles").GetInt32().Should().Be(0);
        json.GetProperty("ranSeconds").GetDouble().Should().Be(2);
        json.GetProperty("windowSeconds").GetDouble().Should().Be(15);
    }

    [Fact]
    public void A_host_that_stopped_early_with_no_logged_exception_is_still_not_a_completed_window()
    {
        var json = Parse(BackgroundAgentDaemonCommand.HostStoppedItselfJson(
            new DaemonFaultLog(), ran: TimeSpan.FromSeconds(1), window: TimeSpan.FromSeconds(30), cycles: null));

        json.GetProperty("ok").GetBoolean().Should().BeFalse();
        json.GetProperty("reason").GetString().Should().Be("host_stopped_early");
        json.GetProperty("service").ValueKind.Should().Be(JsonValueKind.Null,
            "inventing a culprit is worse than saying none was logged");
        json.GetProperty("cycles").ValueKind.Should().Be(JsonValueKind.Null,
            "\"I could not count\" must not deserialize as \"nothing ran\"");
    }

    [Fact]
    public void A_completed_window_that_ran_nothing_says_so()
    {
        var json = Parse(BackgroundAgentDaemonCommand.StoppedJson(
            "duration_elapsed", cycles: 0, window: TimeSpan.FromSeconds(15)));

        json.GetProperty("ok").GetBoolean().Should().BeTrue(
            "a short window with no scheduled work is not a failure — but it is not evidence of one either");
        json.GetProperty("cycles").GetInt32().Should().Be(0);
        json.GetProperty("note").GetString().Should().Contain("no agent cycle ran in this window",
            "the reported defect included a valid config that ran zero cycles and reported exactly what a working node does");
        json.GetProperty("note").GetString().Should().Contain("background-agent report",
            "and it names where to look");
    }

    [Fact]
    public void A_completed_window_that_did_work_is_not_annotated()
    {
        var json = Parse(BackgroundAgentDaemonCommand.StoppedJson(
            "duration_elapsed", cycles: 7, window: TimeSpan.FromMinutes(5)));

        json.GetProperty("ok").GetBoolean().Should().BeTrue();
        json.GetProperty("cycles").GetInt32().Should().Be(7);
        json.GetProperty("note").ValueKind.Should().Be(JsonValueKind.Null,
            "an honest run must not be dressed up as suspect — that inversion would be the worse defect");
    }

    [Fact]
    public void An_uncountable_cycle_count_is_reported_as_unknown_not_as_zero()
    {
        BackgroundAgentDaemonCommand.ZeroCycleNote(null).Should().BeNull();
        BackgroundAgentDaemonCommand.DescribeCycles(null).Should().Contain("could not be read");
        BackgroundAgentDaemonCommand.DescribeCycles(0).Should().Contain("ZERO");
        BackgroundAgentDaemonCommand.DescribeCycles(1).Should().Contain("1 agent cycle.");
    }

    [Fact]
    public void The_fault_log_keeps_the_first_exception_and_ignores_error_lines_without_one()
    {
        var log = new DaemonFaultLog();

        log.Capture("Ashlar.Whatever", "an error with no exception", null);
        log.HasFault.Should().BeFalse("a logged error with no exception is not a host fault");

        log.Capture("Ashlar.First", "first", new InvalidOperationException("boom"));
        log.Capture("Ashlar.Second", "second", new InvalidOperationException("cascade"));

        log.Service.Should().Be("Ashlar.First",
            "a fault cascades, and the last message in the cascade is the least informative one");
        log.Reason.Should().Contain("first");
    }

    [Fact]
    public void A_fault_logged_by_the_generic_host_is_attributed_to_the_ashlar_type_that_threw()
    {
        // This is the real shape: BackgroundServiceExceptionBehavior.StopHost logs under
        // Microsoft.Extensions.Hosting.Internal.Host, so the CATEGORY names the host, not the
        // component. The exception's own stack still names the component.
        var log = new DaemonFaultLog();
        Exception captured;
        try
        {
            ThrowFromAnAshlarType();
            return;
        }
        catch (InvalidOperationException ex)
        {
            captured = ex;
        }

        log.Capture("Microsoft.Extensions.Hosting.Internal.Host", "BackgroundService failed", captured);

        log.Service.Should().StartWith("Ashlar.",
            "an operator needs the component, and \"Microsoft.Extensions.Hosting.Internal.Host\" is not one");
    }

    private static void ThrowFromAnAshlarType() =>
        throw new InvalidOperationException("config load failed");
}
