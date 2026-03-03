using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenEe96f816Command : Command
{
    public HelloGenEe96f816Command() : base("hello-gen-ee96f816", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-ee96f816", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}