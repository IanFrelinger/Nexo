using System.Text.Json;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Objectives;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Tool the planner calls when the currently in-progress objective cannot be
/// driven forward — typically because of an external dependency the planner
/// can't satisfy (e.g. tester offline, missing credentials, ambiguous spec).
///
/// <para>The store moves the objective into the <c>Blocked</c> bucket and
/// records the supplied reason. Operators triage <c>blocked/</c> by listing
/// the folder and using <c>nexo background-agent objectives unblock</c> once
/// the dependency is resolved.</para>
/// </summary>
public sealed class ObjectiveBlockTool : ITool
{
    private readonly IObjectiveStore _store;

    public ObjectiveBlockTool(IObjectiveStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public string Id => "objective.block";
    public ToolSchema Schema => new(Id, "Mark the in-progress objective as blocked with a reason", """
    {"type":"object","required":["id","reason"],"properties":{"id":{"type":"string"},"reason":{"type":"string","description":"Why the objective is blocked (be concrete; operators read this)"}}}
    """);

    private sealed record Args(string id, string reason);

    public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)
            ?? throw new InvalidOperationException("objective.block: empty arguments");
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        try
        {
            var blocked = _store.Block(args.id, args.reason);
            delta.AddLog($"objective.block:{args.id} ({args.reason})");
            return Task.FromResult(new ToolResult(delta, new
            {
                ok = true,
                id = blocked.Id,
                status = blocked.Status.ToString(),
                blocked_reason = blocked.BlockedReason
            }));
        }
        catch (Exception ex)
        {
            delta.AddLog($"objective.block:{args.id} REJECTED ({ex.Message})");
            return Task.FromResult(new ToolResult(delta, new
            {
                ok = false,
                id = args.id,
                error = ex.Message
            }));
        }
    }
}
