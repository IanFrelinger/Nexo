using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen3691288cCommand : Command
{
    public HelloGen3691288cCommand() : base("hello-gen-3691288c", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-3691288c", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}