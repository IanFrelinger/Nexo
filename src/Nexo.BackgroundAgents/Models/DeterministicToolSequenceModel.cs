using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.BackgroundAgents.Models;

/// <summary>
/// One planned tool invocation in a deterministic agent sequence.
/// </summary>
public sealed class ToolSequenceStep
{
    /// <summary>Creates a sequence step.</summary>
    public ToolSequenceStep(
        string toolId,
        JsonElement arguments,
        string? rationale = null,
        Func<bool>? includeWhen = null)
    {
        if (string.IsNullOrWhiteSpace(toolId))
            throw new ArgumentException("Tool id is required.", nameof(toolId));

        ToolId = toolId.Trim();
        Arguments = arguments.ValueKind == JsonValueKind.Undefined
            ? JsonDocument.Parse("{}").RootElement.Clone()
            : arguments.Clone();
        Rationale = rationale;
        IncludeWhen = includeWhen;
    }

    /// <summary>Creates a step from a serializable arguments object.</summary>
    public static ToolSequenceStep FromObject(
        string toolId,
        object? arguments,
        string? rationale = null,
        Func<bool>? includeWhen = null)
    {
        var json = JsonSerializer.SerializeToElement(arguments ?? new { });
        return new ToolSequenceStep(toolId, json, rationale, includeWhen);
    }

    /// <summary>Tool capability id.</summary>
    public string ToolId { get; }

    /// <summary>JSON arguments object.</summary>
    public JsonElement Arguments { get; }

    /// <summary>Optional rationale for this step.</summary>
    public string? Rationale { get; }

    /// <summary>
    /// When non-null, the step is emitted only when the predicate returns true.
    /// Evaluated at emission time.
    /// </summary>
    public Func<bool>? IncludeWhen { get; }
}

/// <summary>
/// One model turn that may emit one or more tool calls.
/// </summary>
public sealed class ToolSequenceTurn
{
    /// <summary>Creates a turn.</summary>
    public ToolSequenceTurn(
        string rationale,
        IReadOnlyList<ToolSequenceStep> steps)
    {
        Rationale = string.IsNullOrWhiteSpace(rationale)
            ? "Execute planned tools."
            : rationale.Trim();
        Steps = steps ?? Array.Empty<ToolSequenceStep>();
    }

    /// <summary>Turn rationale returned to the agent loop.</summary>
    public string Rationale { get; }

    /// <summary>Tool calls for this turn.</summary>
    public IReadOnlyList<ToolSequenceStep> Steps { get; }
}

/// <summary>
/// No-LLM <see cref="IModel"/> that walks a fixed tool-call sequence.
/// Useful for CI, offline agents, and scripted host playtests.
/// </summary>
public sealed class DeterministicToolSequenceModel : IModel
{
    private readonly IReadOnlyList<ToolSequenceTurn> _turns;
    private int _index;

    /// <summary>Creates a model from ordered turns.</summary>
    public DeterministicToolSequenceModel(IReadOnlyList<ToolSequenceTurn> turns)
    {
        _turns = turns ?? throw new ArgumentNullException(nameof(turns));
    }

    /// <summary>Creates a model from a flat list of single-call turns.</summary>
    public static DeterministicToolSequenceModel FromSteps(
        IEnumerable<ToolSequenceStep> steps,
        string defaultRationale = "Execute planned tool.")
    {
        var turns = steps
            .Select(step => new ToolSequenceTurn(
                step.Rationale ?? defaultRationale,
                new[] { step }))
            .ToArray();
        return new DeterministicToolSequenceModel(turns);
    }

    /// <summary>
    /// Loads a JSON array of turns:
    /// <c>[{ "rationale": "...", "tool_calls": [{ "id": "...", "arguments": {} }] }]</c>
    /// </summary>
    public static DeterministicToolSequenceModel FromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Tool sequence JSON must be an array of turns.");

        var turns = new List<ToolSequenceTurn>();
        foreach (var turnElement in document.RootElement.EnumerateArray())
        {
            var rationale = turnElement.TryGetProperty("rationale", out var rationaleElement)
                ? rationaleElement.GetString() ?? "Execute planned tools."
                : "Execute planned tools.";
            if (!turnElement.TryGetProperty("tool_calls", out var calls) ||
                calls.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "Each turn requires a tool_calls array.");
            }

            var steps = new List<ToolSequenceStep>();
            foreach (var call in calls.EnumerateArray())
            {
                var id = call.GetProperty("id").GetString()
                         ?? throw new InvalidOperationException("tool_calls[].id is required.");
                var args = call.TryGetProperty("arguments", out var argsElement)
                    ? argsElement.Clone()
                    : JsonDocument.Parse("{}").RootElement.Clone();
                steps.Add(new ToolSequenceStep(id, args));
            }

            turns.Add(new ToolSequenceTurn(rationale, steps));
        }

        return new DeterministicToolSequenceModel(turns);
    }

    /// <summary>Loads <see cref="FromJson"/> content from a file path.</summary>
    public static DeterministicToolSequenceModel FromJsonFile(string path)
        => FromJson(File.ReadAllText(path));

    /// <inheritdoc />
    public Task<ModelOutput> CompleteAsync(
        ModelInput input,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        while (_index < _turns.Count)
        {
            var turn = _turns[_index++];
            var included = turn.Steps
                .Where(step => step.IncludeWhen?.Invoke() ?? true)
                .Select(step => new
                {
                    id = step.ToolId,
                    arguments = step.Arguments
                })
                .ToArray();

            if (included.Length == 0)
                continue;

            var text = JsonSerializer.Serialize(new
            {
                tool_calls = included,
                rationale = turn.Rationale
            });
            return Task.FromResult(new ModelOutput(text));
        }

        var done = JsonSerializer.Serialize(new
        {
            tool_calls = Array.Empty<object>(),
            rationale = "Deterministic tool sequence completed."
        });
        return Task.FromResult(new ModelOutput(done));
    }
}
