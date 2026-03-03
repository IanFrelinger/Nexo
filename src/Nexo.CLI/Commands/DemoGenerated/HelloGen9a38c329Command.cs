using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen9a38c329Command : Command
{
    public HelloGen9a38c329Command() : base("hello-gen-9a38c329", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-9a38c329", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}