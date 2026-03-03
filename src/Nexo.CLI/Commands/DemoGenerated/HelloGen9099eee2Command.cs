using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen9099eee2Command : Command
{
    public HelloGen9099eee2Command() : base("hello-gen-9099eee2", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-9099eee2", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}