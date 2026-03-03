using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen0a81c7efCommand : Command
{
    public HelloGen0a81c7efCommand() : base("hello-gen-0a81c7ef", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-0a81c7ef", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}