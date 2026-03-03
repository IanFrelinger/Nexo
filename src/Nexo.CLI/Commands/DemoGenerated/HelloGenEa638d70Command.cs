using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenEa638d70Command : Command
{
    public HelloGenEa638d70Command() : base("hello-gen-ea638d70", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-ea638d70", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}