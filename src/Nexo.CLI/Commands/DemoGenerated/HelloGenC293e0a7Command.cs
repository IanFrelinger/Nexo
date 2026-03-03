using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenC293e0a7Command : Command
{
    public HelloGenC293e0a7Command() : base("hello-gen-c293e0a7", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-c293e0a7", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}