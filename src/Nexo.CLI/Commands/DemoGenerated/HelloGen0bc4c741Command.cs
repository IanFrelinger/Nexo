using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen0bc4c741Command : Command
{
    public HelloGen0bc4c741Command() : base("hello-gen-0bc4c741", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-0bc4c741", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}