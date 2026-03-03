using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGen1adb2daaCommand : Command
{
    public HelloGen1adb2daaCommand() : base("hello-gen-1adb2daa", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-1adb2daa", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}