namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Where inference should execute after model selection.
/// </summary>
public enum InferenceTarget
{
    /// <summary>Run inference on this node.</summary>
    Local,

    /// <summary>Delegate inference to a remote node with sufficient capability.</summary>
    Escalate
}
