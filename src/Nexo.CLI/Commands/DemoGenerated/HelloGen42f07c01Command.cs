using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen42f07c01Command : Command
{
    public HelloGen42f07c01Command() : base("hello-gen-42f07c01", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-42f07c01", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}