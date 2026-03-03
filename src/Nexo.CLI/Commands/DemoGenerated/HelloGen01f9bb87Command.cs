using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen01f9bb87Command : Command
{
    public HelloGen01f9bb87Command() : base("hello-gen-01f9bb87", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-01f9bb87", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}