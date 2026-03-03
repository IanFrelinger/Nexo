using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen76c84400Command : Command
{
    public HelloGen76c84400Command() : base("hello-gen-76c84400", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-76c84400", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}