using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenF8dbdaa7Command : Command
{
    public HelloGenF8dbdaa7Command() : base("hello-gen-f8dbdaa7", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-f8dbdaa7", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}