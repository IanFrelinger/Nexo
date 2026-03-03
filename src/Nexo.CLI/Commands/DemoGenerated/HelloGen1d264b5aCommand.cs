using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen1d264b5aCommand : Command
{
    public HelloGen1d264b5aCommand() : base("hello-gen-1d264b5a", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-1d264b5a", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}