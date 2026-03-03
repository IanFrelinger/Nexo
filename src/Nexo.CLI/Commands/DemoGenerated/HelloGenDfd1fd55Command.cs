using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenDfd1fd55Command : Command
{
    public HelloGenDfd1fd55Command() : base("hello-gen-dfd1fd55", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-dfd1fd55", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}