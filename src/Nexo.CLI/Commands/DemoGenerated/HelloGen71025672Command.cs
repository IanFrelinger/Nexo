using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen71025672Command : Command
{
    public HelloGen71025672Command() : base("hello-gen-71025672", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-71025672", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}