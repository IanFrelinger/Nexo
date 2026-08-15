using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.BackgroundAgents.Autonomy;

/// <summary>
/// Identity-only stand-in for a proposed brick. The certification gate needs a brick
/// object to read the id from; with an execution backend present it never executes this
/// instance (witness, determinism and mutation all run in the session), and the swap host
/// reads the real contract off the COMPILED type, so this handle's interface is never
/// consulted.
///
/// <para><c>ExecuteAsync</c> therefore throws rather than returning anything: proposed
/// code must never run in the host process, and if some future path tries, it should fail
/// loudly here instead of quietly executing untrusted code in-proc.</para>
/// </summary>
internal sealed class ProposedBrickHandle : DomainBrick
{
    public ProposedBrickHandle(string brickId)
    {
        Id = brickId;
        Name = brickId;
        Description = "Proposed candidate (identity handle; executes only inside the session).";
        Interface = new BrickInterface { Inputs = [], Outputs = [] };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            $"Proposed brick '{Id}' must execute inside the attested session; in-process "
            + "execution of proposed code is a wiring bug, not a fallback.");
}
