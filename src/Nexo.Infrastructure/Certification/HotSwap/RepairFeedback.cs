using System.Text;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>
/// Projects a rejection into what a PROPOSER is allowed to see, under a
/// <see cref="RepairFeedbackPolicy"/>. The human-facing evidence (the record's reason, the
/// digest, the ledger) is untouched and complete; this is a second, narrower view derived
/// from the same structured findings for a different audience.
///
/// <para>Why a projection and not the reason string: repair feedback that names the
/// expected value leaks the witness to the proposer, and a candidate that converges only
/// once shown the answers converged on the tests, not the contract — its ADMIT is a weaker
/// claim. Redaction here is enforceable; a prompt instruction not to fit the tests is not.
/// The default level shows the proposer only its OWN observed output plus locations
/// (check, case index, key, mutant line), which the evidence tonight says is enough for a
/// small local model to repair a real defect.</para>
/// </summary>
public static class RepairFeedback
{
    /// <summary>Renders proposer-facing feedback for a rejected decision.</summary>
    public static string Render(CertificationDecision decision, RepairFeedbackPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(policy);

        if (decision.Admitted)
            return "certification: ADMIT";

        var check = decision.FailureCheck ?? "unknown";
        var sb = new StringBuilder();
        sb.Append("certification: REJECT at '").Append(check).Append('\'');

        if (policy.Disclosure == RepairDisclosure.CheckOnly)
            return sb.ToString();

        switch (check)
        {
            case "correctness":
                RenderCorrectness(sb, decision, policy);
                break;
            case "mutation":
                RenderMutation(sb, decision, policy);
                break;
            case "analyzer":
                // The analyzer's own feedback is already location-shaped (id, line, col,
                // message) and carries no witness values, so it passes through at every
                // level above CheckOnly — the same verbatim discipline A3 established.
                sb.AppendLine().Append(decision.Record.Reason);
                break;
            default:
                // Determinism, dependency, recursion: the reason strings for these name
                // no witness values, so they are safe as-is.
                sb.AppendLine().Append(decision.Record.Reason);
                break;
        }

        return sb.ToString();
    }

    private static void RenderCorrectness(StringBuilder sb, CertificationDecision decision, RepairFeedbackPolicy policy)
    {
        var findings = decision.WitnessFindings
            .OrderBy(f => f.CaseIndex)
            .ThenBy(f => f.Key, StringComparer.Ordinal)
            .ToList();

        if (findings.Count == 0)
        {
            // Structured findings absent (an older record, or a path that produced only
            // prose): at OwnOutput we cannot safely show the prose because it contains
            // expected values, so we say only that the witness failed.
            sb.AppendLine().Append("  the witness failed; the contract in the objective is authoritative");
            return;
        }

        sb.Append(" — ").Append(findings.Count).Append(" witness finding(s):");
        foreach (var f in findings.Take(policy.MaxFindings))
        {
            sb.AppendLine().Append("  case ").Append(f.CaseIndex).Append(": ");
            switch (f.Kind)
            {
                case WitnessFindingKind.Threw:
                    sb.Append("execution threw");
                    if (f.Detail is not null) sb.Append(" — ").Append(f.Detail);
                    break;
                case WitnessFindingKind.NoObservation:
                    sb.Append("no result was observed for this case");
                    break;
                case WitnessFindingKind.MissingKey:
                    sb.Append("output['").Append(f.Key).Append("'] was not produced");
                    if (policy.Disclosure == RepairDisclosure.Full && f.Expected is not null)
                        sb.Append(" (expected ").Append(f.Expected).Append(')');
                    break;
                case WitnessFindingKind.Mismatch:
                    sb.Append("output['").Append(f.Key).Append("'] violates the contract; you produced ")
                      .Append(f.Actual ?? "<null>");
                    if (policy.Disclosure == RepairDisclosure.Full && f.Expected is not null)
                        sb.Append(" (expected ").Append(f.Expected).Append(')');
                    break;
            }
        }

        if (findings.Count > policy.MaxFindings)
        {
            sb.AppendLine().Append("  (deterministically truncated: ")
              .Append(findings.Count - policy.MaxFindings)
              .Append(" further finding(s) omitted, in case order)");
        }
    }

    private static void RenderMutation(StringBuilder sb, CertificationDecision decision, RepairFeedbackPolicy policy)
    {
        var record = decision.Record;
        sb.Append(" — escape_rate=").Append((record.EscapeRate ?? 0d).ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
          .Append(", ").Append(record.SurvivingMutants).Append(" surviving mutant(s)");

        // A mutant id encodes an operator and a source line: a LOCATION, not an answer.
        // Safe at every level; switchable only because some proposers do worse when told
        // where to look.
        if (policy.IncludeMutantLocations && record.SurvivingMutantIds.Count > 0)
        {
            sb.AppendLine().Append("  survivors: ")
              .Append(string.Join(", ", record.SurvivingMutantIds.Take(policy.MaxFindings)));
            if (record.SurvivingMutantIds.Count > policy.MaxFindings)
                sb.Append(", … (").Append(record.SurvivingMutantIds.Count - policy.MaxFindings).Append(" more)");
        }

        sb.AppendLine().Append("  a surviving mutant means a change to your code that the witness could not detect; ")
          .Append("make the behaviour at that location observable through the declared outputs");
    }
}
