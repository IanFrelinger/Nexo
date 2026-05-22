using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.SelfExtendGenerated;

public sealed class UiShellExtensionCommand : Command, IComposableExtensionCommand
{
    public UiShellExtensionCommand() : base("ext-ui-shell", "Self-extend generated extension command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                command = Name,
                extensionId = ExtensionId,
                dependencies = Dependencies
            }));
            Environment.ExitCode = 0;
        });
    }

    public string ExtensionId => "ui-shell";
    public IReadOnlyList<string> Dependencies { get; } = new[] { "domain-knowledge" };
}