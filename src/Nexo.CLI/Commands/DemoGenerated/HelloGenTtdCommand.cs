using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenTtdCommand : Command
{
    public HelloGenTtdCommand() : base("hello-gen-ttd", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-ttd", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}