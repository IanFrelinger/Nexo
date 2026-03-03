using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen0d8188e2Command : Command
{
    public HelloGen0d8188e2Command() : base("hello-gen-0d8188e2", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-0d8188e2", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}