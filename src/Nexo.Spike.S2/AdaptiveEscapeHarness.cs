using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S2.Adversary;
using Nexo.Spike.S2.Gates;
using Nexo.Spike.S2.Oracle;
using Nexo.Spike.S2.Reporting;
using Nexo.Spike.S1.Transforms;

namespace Nexo.Spike.S2;

public sealed class AdaptiveEscapeHarnessOptions
{
    public required int Intents { get; init; }
    public required int AttemptsPerIntent { get; init; }
    public required string OutputDirectory { get; init; }
    public required string IntentPath { get; init; }
    public required string ReferenceOraclePath { get; init; }
    public double MutationThresholdPercent { get; init; } = 80;
    public bool RequireMutation { get; init; } = true;
    public IAdaptiveAdversary? Adversary { get; init; }
}

public sealed class AdaptiveEscapeHarness
{
    public const string ScopeNote =
        "Lower bound w.r.t. this adversary's effort budget and this reference corpus; " +
        "not a universal guarantee of correctness or exhaustive adaptive search.";

    private readonly FullGateStack _gateStack = new();
    private readonly ReferenceOracleJudge _oracleJudge = new();

    public async Task<AdaptiveEscapeReport> RunAsync(
        AdaptiveEscapeHarnessOptions options,
        CancellationToken ct = default)
    {
        var adversary = options.Adversary ?? AdaptiveAdversaryFactory.Create();
        var spec = await BrickSpecLoader.LoadAsync(options.IntentPath, ct).ConfigureAwait(false);
        var oracle = await ReferenceOracleLoader.LoadAsync(options.ReferenceOraclePath, ct)
            .ConfigureAwait(false);

        ReferenceOracleLoader.AssertStrictSuperset(oracle, spec);

        var implementerRole = AdversaryAccessPolicy.CreateImplementerView(spec);
        var workRoot = Path.Combine(options.OutputDirectory, "workspaces");
        Directory.CreateDirectory(workRoot);

        var allAttempts = new List<AdaptiveAttemptRecord>();
        var backlog = new List<NewDefectBacklogEntry>();
        var attemptsToFirstEscape = new List<int?>();
        var intentCount = Math.Max(1, options.Intents);

        for (var intentIndex = 0; intentIndex < intentCount; intentIndex++)
        {
            ct.ThrowIfCancellationRequested();
            var priorVerdicts = new List<GateVerdict>();
            int? firstEscapeAttempt = null;

            for (var attempt = 0; attempt < options.AttemptsPerIntent; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                var sanitizedPrior = priorVerdicts
                    .Select(v => new GateVerdict(
                        v.AttemptIndex,
                        v.CandidateId,
                        v.Outcome,
                        v.RejectedByGate,
                        v.RejectionDetail,
                        v.GatesPassed,
                        OracleDisagreements: null))
                    .ToList();

                var context = new AdaptiveAttemptContext(
                    spec.IntentId,
                    spec,
                    implementerRole,
                    sanitizedPrior,
                    attempt,
                    options.AttemptsPerIntent);

                var candidate = adversary.GenerateNext(context);
                var workspace = Path.Combine(workRoot, $"intent-{intentIndex:D2}-attempt-{attempt:D2}-{candidate.CandidateId}");

                var gateResult = await EvaluateCandidateAsync(
                    candidate,
                    spec,
                    options.IntentPath,
                    workspace,
                    options.MutationThresholdPercent,
                    options.RequireMutation,
                    ct).ConfigureAwait(false);

                AdaptiveOutcomeKind outcome;
                IReadOnlyList<OracleDisagreement>? disagreements = null;
                IReadOnlyList<OracleDisagreement>? heldOutWrong = null;
                IReadOnlyList<string>? gatesSlipped = null;

                if (!gateResult.GateResult.AllPassed)
                {
                    outcome = AdaptiveOutcomeKind.Rejected;
                }
                else
                {
                    gatesSlipped = gateResult.GateResult.GatesPassed;
                    var judgment = await _oracleJudge.JudgeAsync(candidate.ImplementationSource, oracle, ct)
                        .ConfigureAwait(false);
                    disagreements = judgment.Disagreements;
                    heldOutWrong = _oracleJudge.HeldOutDisagreements(judgment, oracle);

                    if (heldOutWrong.Count > 0)
                    {
                        outcome = AdaptiveOutcomeKind.TrueEscape;
                        firstEscapeAttempt ??= attempt + 1;
                        foreach (var wrong in heldOutWrong)
                        {
                            backlog.Add(new NewDefectBacklogEntry(
                                wrong.Inputs,
                                wrong.ExpectedType,
                                wrong.ActualType,
                                candidate.CandidateId,
                                attempt,
                                candidate.Hypothesis));
                        }
                    }
                    else
                    {
                        outcome = AdaptiveOutcomeKind.BenignPass;
                    }
                }

                var verdict = new GateVerdict(
                    attempt,
                    candidate.CandidateId,
                    outcome,
                    gateResult.GateResult.FailedGate,
                    gateResult.GateResult.Detail,
                    gatesSlipped,
                    disagreements);

                priorVerdicts.Add(verdict);
                allAttempts.Add(new AdaptiveAttemptRecord(
                    intentIndex,
                    attempt,
                    candidate.CandidateId,
                    candidate.Hypothesis,
                    outcome,
                    gateResult.GateResult.FailedGate,
                    gateResult.GateResult.Detail,
                    gatesSlipped ?? [],
                    disagreements ?? [],
                    heldOutWrong ?? []));
            }

            attemptsToFirstEscape.Add(firstEscapeAttempt);
        }

        var total = allAttempts.Count;
        var trueEscapes = allAttempts.Count(a => a.Outcome == AdaptiveOutcomeKind.TrueEscape);
        var benignPasses = allAttempts.Count(a => a.Outcome == AdaptiveOutcomeKind.BenignPass);
        var rejected = allAttempts.Count(a => a.Outcome == AdaptiveOutcomeKind.Rejected);

        return new AdaptiveEscapeReport(
            AdaptiveEscapeReport.Version,
            oracle.Version,
            adversary.BackendName,
            Environment.GetEnvironmentVariable("NEXO_S2_LLM_MODEL"),
            new EffortBudget(intentCount, options.AttemptsPerIntent),
            ScopeNote,
            total == 0 ? 0 : (double)trueEscapes / total,
            total == 0 ? 0 : (double)benignPasses / total,
            total == 0 ? 0 : (double)rejected / total,
            trueEscapes,
            benignPasses,
            rejected,
            attemptsToFirstEscape,
            backlog,
            allAttempts);
    }

    private async Task<(FullGateStackResult GateResult, string Workspace)> EvaluateCandidateAsync(
        AdaptiveCandidate candidate,
        BrickSpec spec,
        string intentPath,
        string workspace,
        double mutationThreshold,
        bool requireMutation,
        CancellationToken ct)
    {
        SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
        await BrickSpecLoader.WriteFrozenAsync(workspace, spec, ct).ConfigureAwait(false);

        File.WriteAllText(
            Path.Combine(workspace, "CsvColumnInferrer", "ColumnTypeInferrer.cs"),
            candidate.ImplementationSource);
        File.WriteAllText(
            Path.Combine(workspace, "CsvColumnInferrer.Tests", "ColumnTypeInferrerRedTests.cs"),
            HonestFixtures.Tests);

        var gateResult = await _gateStack.EvaluateAsync(workspace, mutationThreshold, requireMutation, ct)
            .ConfigureAwait(false);

        return (gateResult, workspace);
    }
}
