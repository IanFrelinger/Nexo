using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenB8549b4fCommand : Command
{
    public HelloGenB8549b4fCommand() : base("hello-gen-b8549b4f", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-b8549b4f", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}