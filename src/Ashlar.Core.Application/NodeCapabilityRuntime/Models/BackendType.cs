namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Model serving backend implementation type.
/// </summary>
public enum BackendType
{
    /// <summary>Ollama local or remote inference server.</summary>
    Ollama,

    /// <summary>llama.cpp optimized for mobile devices.</summary>
    LlamaCppMobile,

    /// <summary>ONNX Runtime for cross-platform model execution.</summary>
    OnnxRuntime
}
