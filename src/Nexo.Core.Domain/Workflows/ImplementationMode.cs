using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Workflows;

/// <summary>
/// Implementation mode for workflows and agents.
/// </summary>
public enum ImplementationMode
{
    /// <summary>Let the runtime choose deterministic or agentic per brick.</summary>
    Auto,

    /// <summary>Force all bricks to use deterministic implementations only.</summary>
    DeterministicOnly,

    /// <summary>Prefer agentic implementations where available.</summary>
    AgenticPreferred,

    /// <summary>Honor per-node and per-brick override settings.</summary>
    Mixed
}
