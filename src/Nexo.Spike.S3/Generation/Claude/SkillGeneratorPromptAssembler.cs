using System.Text.Json;
using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Generation.Claude;

public sealed record AssembledPrompt(string SystemPrompt, string UserPrompt);

/// <summary>
/// Builds Anthropic prompts from the sealed generation request only.
/// </summary>
public static class SkillGeneratorPromptAssembler
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public const string SystemPromptTemplate =
        """
        You are the implementer for a pure C# brick in namespace CsvColumnInferrer.
        Return ONLY a single fenced ```csharp block containing ColumnType enum and ColumnTypeInferrer class.
        Do not include tests, properties, oracle labels, or acceptance examples.
        Honor the sealed intent invariants exactly. No I/O, clocks, or randomness.
        """;

    public static AssembledPrompt Assemble(GenerationRequest request)
    {
        var intentJson = JsonSerializer.Serialize(
            new
            {
                request.Intent,
                request.IntentSpec,
                request.AttemptIndex
            },
            SerializerOptions);

        var retrySection = request.PriorGateVerdicts.Count == 0
            ? "No prior gate failures."
            : string.Join(
                "\n",
                request.PriorGateVerdicts.Select(v =>
                    $"- attempt {v.AttemptIndex}: gate={v.FailedGate ?? "unknown"}; summary={v.RejectionSummary ?? "n/a"}"));

        var userPrompt =
            $"""
             Sealed intent request (no acceptance criteria provided):
             {intentJson}

             Prior gate verdict summaries:
             {retrySection}

             Produce ColumnTypeInferrer implementation source only.
             """;

        return new AssembledPrompt(SystemPromptTemplate, userPrompt);
    }
}
