using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenEf07eafeCommand : Command
{
    public HelloGenEf07eafeCommand() : base("hello-gen-ef07eafe", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-ef07eafe", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}