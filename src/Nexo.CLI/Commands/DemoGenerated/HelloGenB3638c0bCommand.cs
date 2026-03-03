using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenB3638c0bCommand : Command
{
    public HelloGenB3638c0bCommand() : base("hello-gen-b3638c0b", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-b3638c0b", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}