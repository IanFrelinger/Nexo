using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenC4ab239bCommand : Command
{
    public HelloGenC4ab239bCommand() : base("hello-gen-c4ab239b", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-c4ab239b", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}