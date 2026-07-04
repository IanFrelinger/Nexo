using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.SelfExtendGenerated;

/// <summary>CLI command for ui workflow extension.</summary>
public sealed class UiWorkflowExtensionCommand : Command, IComposableExtensionCommand
{
    /// <summary>Creates a new UiWorkflowExtensionCommand instance.</summary>
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

    /// <summary>Stable extension identifier for composition graphs.</summary>
    public string ExtensionId => "ui-workflow";
    /// <summary>Dependencies.</summary>
    public IReadOnlyList<string> Dependencies { get; } = new[] { "domain-knowledge", "ui-shell" };
}