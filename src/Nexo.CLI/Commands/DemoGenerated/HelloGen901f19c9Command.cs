using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen901f19c9Command : Command
{
    public HelloGen901f19c9Command() : base("hello-gen-901f19c9", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-901f19c9", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}