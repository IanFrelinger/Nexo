using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenDa0a6be2Command : Command
{
    public HelloGenDa0a6be2Command() : base("hello-gen-da0a6be2", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-da0a6be2", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}