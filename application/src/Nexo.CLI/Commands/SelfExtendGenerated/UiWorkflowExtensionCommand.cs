using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.SelfExtendGenerated;

public sealed class UiWorkflowExtensionCommand : Command, IComposableExtensionCommand
{
    public UiWorkflowExtensionCommand() : base("ext-ui-workflow", "Self-extend generated extension command")
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

    public string ExtensionId => "ui-workflow";
    public IReadOnlyList<string> Dependencies { get; } = new[] { "domain-knowledge", "ui-shell" };
}