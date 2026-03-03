using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen41f1f51cCommand : Command
{
    public HelloGen41f1f51cCommand() : base("hello-gen-41f1f51c", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-41f1f51c", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}