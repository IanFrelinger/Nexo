using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenE56f31abCommand : Command
{
    public HelloGenE56f31abCommand() : base("hello-gen-e56f31ab", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-e56f31ab", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}