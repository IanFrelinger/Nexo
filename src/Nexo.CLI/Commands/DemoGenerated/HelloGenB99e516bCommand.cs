using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

/// <summary>
/// Demo-generated command created by `nexo demo self-extend`.
/// </summary>
public sealed class HelloGenB99e516bCommand : Command
{
    public HelloGenB99e516bCommand() : base("hello-gen-b99e516b", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-b99e516b", message = "hello from generated command" }));
            Environment.ExitCode = 0;
        });
    }
}