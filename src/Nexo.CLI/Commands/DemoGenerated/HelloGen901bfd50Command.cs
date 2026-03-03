using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen901bfd50Command : Command
{
    public HelloGen901bfd50Command() : base("hello-gen-901bfd50", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-901bfd50", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}