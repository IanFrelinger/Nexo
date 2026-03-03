using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenE9191a4fCommand : Command
{
    public HelloGenE9191a4fCommand() : base("hello-gen-e9191a4f", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-e9191a4f", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}