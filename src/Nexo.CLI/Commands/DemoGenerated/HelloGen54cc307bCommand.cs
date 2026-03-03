using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen54cc307bCommand : Command
{
    public HelloGen54cc307bCommand() : base("hello-gen-54cc307b", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-54cc307b", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}