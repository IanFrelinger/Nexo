namespace Nexo.Bricks.DepExtract;

/// <summary>Structured failure from the DepExtract bricks (maps cleanly to HTTP 400/502).</summary>
public sealed class DepExtractException : Exception
{
    public string Code { get; }
    public int SuggestedStatusCode { get; }

    public DepExtractException(string code, string message, int suggestedStatusCode = 400, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        SuggestedStatusCode = suggestedStatusCode;
    }

    public static DepExtractException InvalidInput(string message) =>
        new("invalid_input", message, 400);

    public static DepExtractException NotFound(string message) =>
        new("not_found", message, 404);

    public static DepExtractException DockerUnavailable(string message, Exception? inner = null) =>
        new("docker_unavailable", message, 502, inner);

    public static DepExtractException ExtractFailed(string message, Exception? inner = null) =>
        new("extract_failed", message, 502, inner);

    public static DepExtractException AdaptFailed(string message, Exception? inner = null) =>
        new("adapt_failed", message, 502, inner);

    public static DepExtractException EmptyModelResponse() =>
        new("empty_model_response", "The local model returned an empty adapter draft.", 502);

    public static DepExtractException CompileFailed(string message) =>
        new("compile_failed", message, 502);
}
