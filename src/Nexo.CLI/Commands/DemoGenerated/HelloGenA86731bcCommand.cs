using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenA86731bcCommand : Command
{
    public HelloGenA86731bcCommand() : base("hello-gen-a86731bc", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-a86731bc", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}