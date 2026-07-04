using Nexo.Infrastructure.Execution;

namespace Nexo.Infrastructure.Execution.LoadPolicy;

/// <summary>
/// Preference for where to execute LLM requests: edge (local) or server (cloud).
/// </summary>
public enum LoadPreference
{
    /// <summary>Prefer local/edge (Ollama, ONNX, LLamaSharp).</summary>
    Edge,

    /// <summary>Prefer server/cloud (OpenAI, Azure).</summary>
    Server,

    /// <summary>Auto: evaluate at runtime (battery, availability, etc.).</summary>
    Auto
}
