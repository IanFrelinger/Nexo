using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen04480915Command : Command
{
    public HelloGen04480915Command() : base("hello-gen-04480915", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-04480915", message = "TODO: fix me" }));
            Environment.ExitCode = 0;
        });
    }
}