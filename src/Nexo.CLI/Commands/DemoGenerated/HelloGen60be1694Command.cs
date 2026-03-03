using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen60be1694Command : Command
{
    public HelloGen60be1694Command() : base("hello-gen-60be1694", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-60be1694", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}