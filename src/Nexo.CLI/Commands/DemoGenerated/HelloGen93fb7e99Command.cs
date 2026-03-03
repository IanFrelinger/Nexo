using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen93fb7e99Command : Command
{
    public HelloGen93fb7e99Command() : base("hello-gen-93fb7e99", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-93fb7e99", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}