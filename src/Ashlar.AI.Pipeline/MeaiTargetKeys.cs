namespace Ashlar.AI.Pipeline;

/// <summary>
/// Well-known keyed DI service keys for MEAI chat targets.
/// </summary>
public static class MeaiTargetKeys
{
    /// <summary>Local Ollama HTTP target.</summary>
    public const string LocalOllama = "local:ollama";

    /// <summary>
    /// Local offline target. Product key retains <c>local:onnx</c> for policy continuity;
    /// the implementation is LLamaSharp + GGUF (see migration notes).
    /// </summary>
    public const string LocalOnnx = "local:onnx";
}
