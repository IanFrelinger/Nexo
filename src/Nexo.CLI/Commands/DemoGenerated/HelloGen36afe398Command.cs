using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen36afe398Command : Command
{
    public HelloGen36afe398Command() : base("hello-gen-36afe398", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-36afe398", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}