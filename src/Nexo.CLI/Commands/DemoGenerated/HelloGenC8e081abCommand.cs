using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenC8e081abCommand : Command
{
    public HelloGenC8e081abCommand() : base("hello-gen-c8e081ab", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-c8e081ab", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}