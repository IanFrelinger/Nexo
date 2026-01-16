using System.Text.Json;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline;

/// <summary>
/// Mutable context passed through the demo command pipeline.
/// Commands mutate this object; orchestrator provides validation + success/failure.
/// </summary>
public sealed class SelfExtendContext
{
    public required string RepoRoot { get; init; }
    public required string OutputRoot { get; init; }
    public required SelfExtendSpec Spec { get; init; }

    public int Iteration { get; set; }
    public bool? LastTestOk { get; set; }
    public string? LastTestStdout { get; set; }
    public string? LastTestStderr { get; set; }

    public List<object> History { get; } = new();

    public string GeneratedCommandInvocation => $"demo {Spec.CommandName}";

    public string GeneratedCommandClassName => ToPascal(Spec.CommandName) + "Command";

    public string GeneratedCommandPath => $"src/Nexo.CLI/Commands/DemoGenerated/{GeneratedCommandClassName}.cs";

    public static string ToPascal(string s)
    {
        var parts = s
            .Replace('_', '-')
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }
}

