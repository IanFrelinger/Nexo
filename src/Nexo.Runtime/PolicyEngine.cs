using System.Security.Cryptography;
using System.Text;
using Nexo.Abstractions;

namespace Nexo.Runtime;

/// <summary>
/// Engine for evaluating policies on tool calls.
/// 
/// Responsibilities:
/// - Evaluates all registered IPolicy instances for tool call approval
/// - Signs action deltas with SHA256 hash for integrity verification
/// - Returns approval status and reason for denials
/// 
/// Used by AgentHost to enforce policies before tool execution.
/// Provides cryptographic signing of action deltas.
/// </summary>
public sealed class PolicyEngine
{
    private readonly IReadOnlyList<IPolicy> _policies;

    /// <summary>Creates a policy engine from the given policies.</summary>
    public PolicyEngine(IEnumerable<IPolicy> policies)
    {
        _policies = policies.ToList();
    }

    /// <summary>Evaluates all policies for a tool call; sets <paramref name="reason"/> on denial.</summary>
    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        foreach (var p in _policies)
        {
            if (!p.Approve(call, s, out reason))
                return false;
        }
        reason = "OK";
        return true;
    }

    /// <summary>Signs an action delta with a SHA-256 integrity hash.</summary>
    public IActionDelta Sign(IActionDelta delta)
    {
        using var sha = SHA256.Create();
        var text = $"{delta.TickFrom}:{delta.TickTo}:{string.Join("|", delta.Log)}";
        var sig = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        delta.Signature = sig;
        return delta;
    }
}
