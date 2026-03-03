using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenDd90f4d1Command : Command
{
    public HelloGenDd90f4d1Command() : base("hello-gen-dd90f4d1", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-dd90f4d1", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}