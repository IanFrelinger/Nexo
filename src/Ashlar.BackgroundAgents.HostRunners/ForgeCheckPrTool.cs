using System.Text.Json;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.Tools.Dev.Deltas;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// Tool the planner calls to look up the current status of a proposal it raised
/// in an earlier cycle. Lets the agent decide whether to retry, escalate, or
/// move on to the next objective without waiting on a side-channel signal.
///
/// <para>Naming uses "pr" rather than "proposal" because that's the language the
/// agent prompt uses (and what operators are likely to type) — the
/// implementation maps it onto the proposal store under the hood.</para>
/// </summary>
public sealed class ForgeCheckPrTool : ITool
{
    private readonly IChangeProposalStore _store;

    public ForgeCheckPrTool(IChangeProposalStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public string Id => "forge.check_pr";

    public ToolSchema Schema => new(Id,
        "Check the status of a previously submitted forge.propose_change proposal",
        """
        {"type":"object","required":["id"],"properties":{"id":{"type":"string","description":"Proposal id returned by forge.propose_change"}}}
        """);

    private sealed record Args(string id);

    public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)
                   ?? throw new InvalidOperationException("forge.check_pr: empty arguments");
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };

        try
        {
            var proposal = _store.Find(args.id);
            if (proposal is null)
            {
                delta.AddLog($"forge.check_pr:{args.id} NOT_FOUND");
                return Task.FromResult(new ToolResult(delta, new
                {
                    ok = false,
                    id = args.id,
                    error = "Proposal not found."
                }));
            }

            delta.AddLog($"forge.check_pr:{args.id} status={proposal.Status}");
            return Task.FromResult(new ToolResult(delta, new
            {
                ok = true,
                id = proposal.Id,
                status = proposal.Status.ToString(),
                target_path = proposal.TargetPath,
                summary = proposal.Summary,
                resolved_by = proposal.ResolvedBy,
                resolution_note = proposal.ResolutionNote,
                created_at = proposal.CreatedAt,
                updated_at = proposal.UpdatedAt
            }));
        }
        catch (Exception ex)
        {
            delta.AddLog($"forge.check_pr:{args.id} ERROR ({ex.Message})");
            return Task.FromResult(new ToolResult(delta, new
            {
                ok = false,
                id = args.id,
                error = ex.Message
            }));
        }
    }
}
