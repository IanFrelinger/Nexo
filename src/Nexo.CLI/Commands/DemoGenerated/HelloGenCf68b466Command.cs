using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenCf68b466Command : Command
{
    public HelloGenCf68b466Command() : base("hello-gen-cf68b466", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-cf68b466", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}