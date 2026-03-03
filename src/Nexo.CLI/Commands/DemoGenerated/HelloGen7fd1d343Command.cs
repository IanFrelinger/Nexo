using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen7fd1d343Command : Command
{
    public HelloGen7fd1d343Command() : base("hello-gen-7fd1d343", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-7fd1d343", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}