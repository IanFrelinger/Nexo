using System.Security.Cryptography;
using System.Text;
using Nexo.Abstractions;

namespace Nexo.Runtime;

public sealed class PolicyEngine
{
    private readonly IReadOnlyList<IPolicy> _policies;

    public PolicyEngine(IEnumerable<IPolicy> policies)
    {
        _policies = policies.ToList();
    }

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

    public IActionDelta Sign(IActionDelta delta)
    {
        using var sha = SHA256.Create();
        var text = $"{delta.TickFrom}:{delta.TickTo}:{string.Join('|', delta.Log)}";
        var sig = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        delta.Signature = sig;
        return delta;
    }
}
