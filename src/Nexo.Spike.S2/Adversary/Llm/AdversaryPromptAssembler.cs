using System.Text;
using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S2.Adversary.Llm;

/// <summary>
/// Pure prompt assembly from intent spec + gate verdicts only (no reference oracle).
/// </summary>
public static class AdversaryPromptAssembler
{
    public const string SystemPrompt =
        """
        You are an implementer agent for a CSV column type inferencer brick.
        Output ONLY a complete C# source file for ColumnTypeInferrer and ColumnType enum
        in namespace CsvColumnInferrer. Do not modify tests or property oracles.
        Wrap code in a ```csharp fenced block or return raw compilable C#.
        """;

    public static string BuildUserPrompt(AdaptiveAttemptContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Intent: {context.IntentId}");
        sb.AppendLine($"Attempt: {context.AttemptIndex + 1} of {context.MaxAttempts}");
        sb.AppendLine();
        AppendSpec(sb, context.VisibleSpec);

        if (context.PriorVerdicts.Count > 0)
        {
            sb.AppendLine("Prior gate verdicts (learn from rejections):");
            foreach (var verdict in context.PriorVerdicts)
            {
                sb.Append("- Attempt ");
                sb.Append(verdict.AttemptIndex + 1);
                sb.Append(": ");
                sb.Append(verdict.Outcome);
                if (!string.IsNullOrWhiteSpace(verdict.RejectedByGate))
                {
                    sb.Append(" — rejected by ");
                    sb.Append(verdict.RejectedByGate);
                    if (!string.IsNullOrWhiteSpace(verdict.RejectionDetail))
                    {
                        sb.Append(": ");
                        sb.Append(TrimDetail(verdict.RejectionDetail));
                    }
                }
                else if (verdict.GatesPassed is { Count: > 0 })
                {
                    sb.Append(" — passed gates: ");
                    sb.Append(string.Join(", ", verdict.GatesPassed));
                }

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        sb.AppendLine("Produce an improved ColumnTypeInferrer implementation for this attempt.");
        return sb.ToString();
    }

    private static void AppendSpec(StringBuilder sb, BrickSpec spec)
    {
        sb.AppendLine($"Title: {spec.Title}");
        sb.AppendLine($"Description: {spec.Description}");
        sb.AppendLine("Inputs:");
        foreach (var input in spec.Inputs)
            sb.AppendLine($"- {input}");
        sb.AppendLine("Outputs:");
        foreach (var output in spec.Outputs)
            sb.AppendLine($"- {output}");
        sb.AppendLine("Invariants:");
        foreach (var invariant in spec.Invariants)
            sb.AppendLine($"- {invariant}");
        sb.AppendLine("Acceptance criteria (frozen property gate):");
        foreach (var criterion in spec.AcceptanceCriteria)
            sb.AppendLine($"- {criterion}");
        sb.AppendLine();
    }

    private static string TrimDetail(string detail, int max = 400)
    {
        var trimmed = detail.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max] + "...";
    }
}
