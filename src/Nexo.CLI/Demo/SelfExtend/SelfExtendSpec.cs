namespace Nexo.CLI.Demo.SelfExtend;

/// <summary>
/// Configuration for the self-extension demo.
/// Defines the command/behavior to generate and the expected output contract for tests.
/// </summary>
public sealed record SelfExtendSpec
{
    public required string CommandName { get; init; }          // e.g. "hello-gen"
    public required string Message { get; init; }              // e.g. "hello from generated command"
    public string? Category { get; init; } = "DemoSelfExtend"; // test category label
}

