using Nexo.Spike.S0.Agents;
using Nexo.Spike.S0.Guards;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;

namespace Nexo.Spike.S0;

/// <summary>
/// Synchronous SPEC → RED → GREEN → VERIFY loop with role separation and mutation gate.
/// </summary>
public sealed class TddLoopRunner
{
    private readonly IStageAgent _testAuthor;
    private readonly IStageAgent _implementer;
    private readonly PropertyGate _propertyGate;
    private readonly MutationGate _mutationGate;

    public TddLoopRunner()
        : this(
            new FileBasedStageAgent("test-author"),
            new FileBasedStageAgent("implementer"),
            new PropertyGate(),
            new MutationGate())
    {
    }

    public TddLoopRunner(
        IStageAgent testAuthor,
        IStageAgent implementer,
        MutationGate mutationGate)
        : this(testAuthor, implementer, new PropertyGate(), mutationGate)
    {
    }

    public TddLoopRunner(
        IStageAgent testAuthor,
        IStageAgent implementer,
        PropertyGate propertyGate,
        MutationGate mutationGate)
    {
        _testAuthor = testAuthor;
        _implementer = implementer;
        _propertyGate = propertyGate;
        _mutationGate = mutationGate;
    }

    public async Task<SpikeRunLog> RunAsync(SpikeLoopOptions options, CancellationToken ct = default)
    {
        var runLog = new SpikeRunLog
        {
            RunId = options.RunId ?? Guid.NewGuid().ToString("N"),
            IntentId = Path.GetFileNameWithoutExtension(options.IntentPath),
            StartedAt = DateTimeOffset.UtcNow,
            Outcome = SpikeRunOutcome.Blocked,
            RejectingGuard = GuardKind.None
        };

        var workspace = options.WorkspaceRoot;
        if (!Directory.Exists(Path.Combine(workspace, "CsvColumnInferrer")))
            SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: Directory.Exists(workspace));

        await SpikeWorkspaceScaffold.InitGitAsync(workspace, ct).ConfigureAwait(false);

        var spec = await BrickSpecLoader.LoadAsync(options.IntentPath, ct).ConfigureAwait(false);
        if (spec.HasOpenQuestions)
        {
            runLog.RejectingGuard = GuardKind.OpenQuestions;
            runLog.RejectedAtStage = SpikeStage.Spec;
            runLog.Outcome = SpikeRunOutcome.Blocked;
            runLog.RecordStage(SpikeStage.Spec, "Spec has open questions — surface to human.");
            await FinalizeAsync(workspace, runLog, ct).ConfigureAwait(false);
            return runLog;
        }

        await BrickSpecLoader.WriteFrozenAsync(workspace, spec, ct).ConfigureAwait(false);
        runLog.RecordStage(SpikeStage.Spec, "Frozen spec written.");
        if (options.CommitPerStage)
            await SpikeWorkspaceScaffold.CommitStageAsync(workspace, SpikeStage.Spec, ct).ConfigureAwait(false);

        IReadOnlyList<string>? mutationFeedback = null;
        while (runLog.RedGreenBounces <= options.MaxRedGreenBounces)
        {
            var redResult = await RunRedAsync(spec, workspace, options, runLog, ct).ConfigureAwait(false);
            if (!redResult)
            {
                await FinalizeAsync(workspace, runLog, ct).ConfigureAwait(false);
                return runLog;
            }

            var greenResult = await RunGreenAsync(spec, workspace, options, runLog, ct).ConfigureAwait(false);
            if (!greenResult)
            {
                await FinalizeAsync(workspace, runLog, ct).ConfigureAwait(false);
                return runLog;
            }

            var verifyResult = await RunVerifyAsync(workspace, options, runLog, ct).ConfigureAwait(false);
            if (verifyResult.Passed)
            {
                runLog.Outcome = SpikeRunOutcome.Success;
                runLog.RejectingGuard = GuardKind.None;
                await FinalizeAsync(workspace, runLog, ct).ConfigureAwait(false);
                return runLog;
            }

            mutationFeedback = verifyResult.SurvivingMutants;
            if (runLog.RedGreenBounces >= options.MaxRedGreenBounces)
            {
                if (runLog.Outcome != SpikeRunOutcome.Rejected)
                {
                    runLog.Outcome = SpikeRunOutcome.BudgetExhausted;
                    runLog.RejectingGuard = GuardKind.Budget;
                    runLog.RecordStage(SpikeStage.Verify, "RED↔GREEN bounce budget exhausted.");
                }

                break;
            }

            runLog.RedGreenBounces++;
            runLog.RecordStage(SpikeStage.Verify, "VERIFY failed — bouncing to RED.", new
            {
                rejectingGuard = runLog.RejectingGuard,
                mutationFeedback
            });
        }

        await FinalizeAsync(workspace, runLog, ct).ConfigureAwait(false);
        return runLog;
    }

    private async Task<bool> RunRedAsync(
        BrickSpec spec,
        string workspace,
        SpikeLoopOptions options,
        SpikeRunLog runLog,
        CancellationToken ct)
    {
        var testScope = RoleScope.ForTestAuthor(spec);
        var agentResult = await _testAuthor.ExecuteAsync(SpikeStage.Red, testScope, workspace, null, ct)
            .ConfigureAwait(false);
        if (!agentResult.Success)
        {
            runLog.Outcome = SpikeRunOutcome.Blocked;
            runLog.RejectedAtStage = SpikeStage.Red;
            runLog.RecordStage(SpikeStage.Red, agentResult.Summary, agentResult.OpenQuestions);
            return (false);
        }

        var (buildCode, buildOut, buildErr, buildTimedOut) =
            await SpikeWorkspaceScaffold.BuildAsync(workspace, ct).ConfigureAwait(false);
        if (buildCode != 0 || buildTimedOut)
        {
            runLog.Outcome = SpikeRunOutcome.Rejected;
            runLog.RejectingGuard = GuardKind.RedReason;
            runLog.RejectedAtStage = SpikeStage.Red;
            runLog.RecordStage(SpikeStage.Red, "Build failed before RED check.", new { buildOut, buildErr });
            return (false);
        }

        var (testCode, testOut, testErr, testTimedOut) =
            await SpikeWorkspaceScaffold.TestAsync(workspace, ct).ConfigureAwait(false);
        var reason = RedReasonClassifier.Classify(testCode, testOut, testErr, testTimedOut);

        runLog.RecordStage(SpikeStage.Red, "RED reason classified.", new
        {
            reason,
            testCode
        });

        if (TautologyGuard.IsTautological(reason))
        {
            runLog.Outcome = SpikeRunOutcome.Rejected;
            runLog.RejectingGuard = GuardKind.Tautology;
            runLog.RejectedAtStage = SpikeStage.Red;
            return (false);
        }

        if (reason == RedFailureReason.Unimplemented)
        {
            runLog.Outcome = SpikeRunOutcome.Rejected;
            runLog.RejectingGuard = GuardKind.RedReason;
            runLog.RejectedAtStage = SpikeStage.Red;
            return (false);
        }

        if (reason != RedFailureReason.AssertionFailure)
        {
            runLog.Outcome = SpikeRunOutcome.Rejected;
            runLog.RejectingGuard = GuardKind.RedReason;
            runLog.RejectedAtStage = SpikeStage.Red;
            return (false);
        }

        if (options.CommitPerStage)
            await SpikeWorkspaceScaffold.CommitStageAsync(workspace, SpikeStage.Red, ct).ConfigureAwait(false);

        return (true);
    }

    private async Task<bool> RunGreenAsync(
        BrickSpec spec,
        string workspace,
        SpikeLoopOptions options,
        SpikeRunLog runLog,
        CancellationToken ct)
    {
        var implScope = RoleScope.ForImplementer(spec);
        var agentResult = await _implementer.ExecuteAsync(SpikeStage.Green, implScope, workspace, null, ct)
            .ConfigureAwait(false);
        if (!agentResult.Success)
        {
            runLog.Outcome = SpikeRunOutcome.Blocked;
            runLog.RejectedAtStage = SpikeStage.Green;
            runLog.RecordStage(SpikeStage.Green, agentResult.Summary, agentResult.OpenQuestions);
            return (false);
        }

        SpikeWorkspaceScaffold.CleanBuildArtifacts(workspace);

        var (buildCode, buildOut, buildErr, buildTimedOut) =
            await SpikeWorkspaceScaffold.BuildAsync(workspace, ct).ConfigureAwait(false);
        if (buildCode != 0 || buildTimedOut)
        {
            runLog.Outcome = SpikeRunOutcome.Rejected;
            runLog.RejectedAtStage = SpikeStage.Green;
            runLog.RecordStage(SpikeStage.Green, "Implementation does not build.", new { buildOut, buildErr });
            return (false);
        }

        var (testCode, testOut, testErr, testTimedOut) =
            await SpikeWorkspaceScaffold.TestWithRebuildAsync(workspace, ct).ConfigureAwait(false);
        if (testCode != 0 || testTimedOut)
        {
            runLog.Outcome = SpikeRunOutcome.Rejected;
            runLog.RejectedAtStage = SpikeStage.Green;
            runLog.RecordStage(SpikeStage.Green, "Tests not green.", new { testOut, testErr });
            return (false);
        }

        runLog.RecordStage(SpikeStage.Green, "All tests green.");
        if (options.CommitPerStage)
            await SpikeWorkspaceScaffold.CommitStageAsync(workspace, SpikeStage.Green, ct).ConfigureAwait(false);

        return (true);
    }

    private async Task<(bool Passed, IReadOnlyList<string> SurvivingMutants)> RunVerifyAsync(
        string workspace,
        SpikeLoopOptions options,
        SpikeRunLog runLog,
        CancellationToken ct)
    {
        var propertyPassed = true;
        if (!options.SkipProperty)
        {
            var propertyResult = await _propertyGate.RunAsync(workspace, ct).ConfigureAwait(false);
            runLog.RecordProperty(propertyResult.Passed, propertyResult.FailedTests, propertyResult.RawOutput);
            runLog.RecordStage(SpikeStage.Verify, "Property/metamorphic gate complete.", new
            {
                propertyResult.Passed,
                propertyResult.FailedTests
            });

            if (!propertyResult.Passed)
            {
                runLog.Outcome = SpikeRunOutcome.Rejected;
                runLog.RejectingGuard = GuardKind.Property;
                runLog.RejectedAtStage = SpikeStage.Verify;
                propertyPassed = false;
            }
        }
        else
        {
            runLog.RecordStage(SpikeStage.Verify, "Property gate skipped (--skip-property).");
        }

        if (!propertyPassed)
        {
            if (options.CommitPerStage)
                await SpikeWorkspaceScaffold.CommitStageAsync(workspace, SpikeStage.Verify, ct).ConfigureAwait(false);
            return (false, []);
        }

        if (options.SkipMutation)
        {
            runLog.RecordStage(SpikeStage.Verify, "Mutation skipped (--skip-mutation).");
            runLog.RecordVerify(100, [], true);
            if (options.CommitPerStage)
                await SpikeWorkspaceScaffold.CommitStageAsync(workspace, SpikeStage.Verify, ct).ConfigureAwait(false);
            return (true, []);
        }

        var result = await _mutationGate.RunAsync(workspace, options.MutationThresholdPercent, ct)
            .ConfigureAwait(false);
        runLog.RecordVerify(result.MutationScore, result.SurvivingMutants, result.Passed);
        runLog.RecordStage(SpikeStage.Verify, "Stryker mutation run complete.", new
        {
            result.MutationScore,
            result.Passed,
            result.SurvivingMutants
        });

        if (!result.Passed)
        {
            runLog.Outcome = SpikeRunOutcome.Rejected;
            runLog.RejectingGuard = GuardKind.Mutation;
            runLog.RejectedAtStage = SpikeStage.Verify;
        }

        if (options.CommitPerStage)
            await SpikeWorkspaceScaffold.CommitStageAsync(workspace, SpikeStage.Verify, ct).ConfigureAwait(false);

        return (result.Passed, result.SurvivingMutants);
    }

    private static async Task FinalizeAsync(string workspace, SpikeRunLog runLog, CancellationToken ct)
    {
        runLog.CompletedAt = DateTimeOffset.UtcNow;
        await SpikeWorkspaceScaffold.WriteRunLogAsync(workspace, runLog, ct).ConfigureAwait(false);
    }
}
