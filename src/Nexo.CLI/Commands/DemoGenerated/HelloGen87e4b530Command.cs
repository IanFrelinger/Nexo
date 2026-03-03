using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen87e4b530Command : Command
{
    public HelloGen87e4b530Command() : base("hello-gen-87e4b530", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-87e4b530", message = "TODO: fix me" }));
            Environment.ExitCode = 0;
        });
    }
}