using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen95211ed7Command : Command
{
    public HelloGen95211ed7Command() : base("hello-gen-95211ed7", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-95211ed7", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}