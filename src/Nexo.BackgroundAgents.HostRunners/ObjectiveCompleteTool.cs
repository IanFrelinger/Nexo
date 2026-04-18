using System.Text.Json;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Objectives;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Tool the planner calls when it believes the currently in-progress objective is
/// finished. Performs the lifecycle move <c>InProgress → Done</c> against the
/// shared <see cref="IObjectiveStore"/>, optionally appending a completion note
/// to the objective body so operators have an audit trail.
///
/// <para>Naming: <c>objective.complete</c>. Schema is intentionally minimal — the
/// id is required (we don't infer it from snapshot to keep the LLM honest) and
/// note is optional.</para>
/// </summary>
public sealed class ObjectiveCompleteTool : ITool
{
    private readonly IObjectiveStore _store;

    public ObjectiveCompleteTool(IObjectiveStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public string Id => "objective.complete";
    public ToolSchema Schema => new(Id, "Mark the in-progress objective as done", """
    {"type":"object","required":["id"],"properties":{"id":{"type":"string","description":"Objective id (matches the markdown filename without .md)"},"note":{"type":"string","description":"Optional completion note appended to the objective body"}}}
    """);

    private sealed record Args(string id, string? note);

    public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)
            ?? throw new InvalidOperationException("objective.complete: empty arguments");
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        try
        {
            var done = _store.Complete(args.id, args.note);
            delta.AddLog($"objective.complete:{args.id} done");
            return Task.FromResult(new ToolResult(delta, new
            {
                ok = true,
                id = done.Id,
                title = done.Title,
                status = done.Status.ToString()
            }));
        }
        catch (Exception ex)
        {
            // Surface the failure as a structured tool result rather than throwing,
            // so the LLM can see the reason (typically: "expected InProgress") and
            // recover instead of exiting the cycle on an exception.
            delta.AddLog($"objective.complete:{args.id} REJECTED ({ex.Message})");
            return Task.FromResult(new ToolResult(delta, new
            {
                ok = false,
                id = args.id,
                error = ex.Message
            }));
        }
    }
}
