using System.CommandLine;
using System.Text.Json;

namespace Ashlar.CLI.Commands.SelfExtendGenerated;

/// <summary>CLI command for ui shell extension.</summary>
public sealed class UiShellExtensionCommand : Command, IComposableExtensionCommand
{
    /// <summary>Creates a new UiShellExtensionCommand instance.</summary>
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

    /// <summary>Stable extension identifier for composition graphs.</summary>
    public string ExtensionId => "ui-shell";
    /// <summary>Dependencies.</summary>
    public IReadOnlyList<string> Dependencies { get; } = new[] { "domain-knowledge" };
}