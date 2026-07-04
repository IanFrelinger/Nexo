namespace Nexo.CLI.Commands.Workflow;

internal sealed record ExecutionTarget(
    string Id,
    string? Endpoint,
    bool IsLocal)
{
    public static readonly ExecutionTarget Local = new("local", null, true);
}
