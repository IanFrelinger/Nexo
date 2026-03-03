using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen5d819121Command : Command
{
    public HelloGen5d819121Command() : base("hello-gen-5d819121", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-5d819121", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}